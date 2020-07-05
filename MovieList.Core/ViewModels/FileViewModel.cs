using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

using DynamicData;

using MovieList.Data;
using MovieList.Data.Models;
using MovieList.Data.Services;
using MovieList.DialogModels;
using MovieList.Models;
using MovieList.ViewModels.Forms.Preferences;

using ReactiveUI;
using ReactiveUI.Fody.Helpers;

using Splat;

namespace MovieList.ViewModels
{
    public sealed class FileViewModel : ReactiveObject
    {
        private readonly SourceCache<Kind, int> kindsSource;
        private readonly ReadOnlyObservableCollection<Kind> kinds;
        private readonly CompositeDisposable currentContentSubscriptions = new CompositeDisposable();
        private readonly ISettingsService settingsService;

        public FileViewModel(
            string fileName,
            string listName,
            IKindService? kindService = null,
            ISettingsService? settingsService = null)
        {
            this.FileName = fileName;
            this.ListName = listName;

            kindService ??= Locator.Current.GetService<IKindService>(fileName);
            this.settingsService = settingsService ?? Locator.Current.GetService<ISettingsService>(fileName);

            this.Header = new TabHeaderViewModel(this.FileName, this.ListName);

            this.kindsSource = new SourceCache<Kind, int>(kind => kind.Id);

            this.WhenAnyValue(vm => vm.Content)
                .Select(content => content is FileMainContentViewModel
                    ? this.WhenAnyValue(vm => vm.MainContent!.AreUnsavedChangesPresent)
                    : this.WhenAnyValue(vm => vm.Settings!.IsFormChanged))
                .Switch()
                .ToPropertyEx(this, vm => vm.AreUnsavedChangesPresent, initialValue: false);

            this.kindsSource.Connect()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out this.kinds)
                .DisposeMany()
                .Subscribe();

            kindService.GetAllKindsInTaskPool()
                .Do(this.kindsSource.AddOrUpdate)
                .Discard()
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(this.SwitchCurrentContentToMain);

            this.SwitchToList = ReactiveCommand.CreateFromObservable(this.OnSwitchToList);
            this.SwitchToStats = ReactiveCommand.Create(() => { });
            this.SwitchToSettings = ReactiveCommand.CreateFromObservable(this.OnSwitchToSettings);
            this.UpdateSettings = ReactiveCommand.Create<SettingsModel, SettingsModel>(this.OnUpdateSettings);
            this.Save = ReactiveCommand.Create(() => { });

            this.WhenAnyValue(vm => vm.ListName)
                .BindTo(this.Header, h => h.TabName);
        }

        public string FileName { get; }

        [Reactive]
        public string ListName { get; set; }

        public TabHeaderViewModel Header { get; }

        [Reactive]
        public ReactiveObject Content { get; set; } = null!;

        public FileMainContentViewModel? MainContent { get; private set; }
        public SettingsFormViewModel? Settings { get; private set; }

        public ReadOnlyObservableCollection<Kind> Kinds
            => this.kinds;

        public bool AreUnsavedChangesPresent { [ObservableAsProperty] get; }

        public ReactiveCommand<Unit, Unit> SwitchToList { get; }
        public ReactiveCommand<Unit, Unit> SwitchToStats { get; }
        public ReactiveCommand<Unit, Unit> SwitchToSettings { get; }
        public ReactiveCommand<SettingsModel, SettingsModel> UpdateSettings { get; }
        public ReactiveCommand<Unit, Unit> Save { get; }

        private SettingsModel OnUpdateSettings(SettingsModel settingsModel)
        {
            this.kindsSource.Edit(list =>
            {
                list.Clear();
                list.AddOrUpdate(settingsModel.Kinds);
            });

            this.Header.TabName = settingsModel.Settings.ListName;

            return settingsModel;
        }

        private IObservable<Unit> OnSwitchToList()
        {
            var canSwitch = this.Settings?.IsFormChanged ?? false
                ? Dialog.Confirm.Handle(new ConfirmationModel("CloseForm"))
                : Observable.Return(true);

            return canSwitch
                .ObserveOn(RxApp.MainThreadScheduler)
                .DoIfTrue(this.SwitchCurrentContentToMain)
                .Discard();
        }

        private void SwitchCurrentContentToMain()
        {
            this.currentContentSubscriptions.Clear();
            this.Settings = null;

            this.Content = this.MainContent = new FileMainContentViewModel(this.FileName, this.Kinds);

            this.Save
                .InvokeCommand(this.MainContent.TunnelSave)
                .DisposeWith(this.currentContentSubscriptions);
        }

        private IObservable<Unit> OnSwitchToSettings()
        {
            var observable = this.MainContent?.AreUnsavedChangesPresent ?? false
                ? Dialog.Confirm.Handle(new ConfirmationModel("CloseForm"))
                : Observable.Return(true);

            return observable
                .SelectMany(canSwitch => canSwitch
                    ? this.settingsService.GetSettingsInTaskPool()
                    : Observable.Return<Settings?>(null))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Do(this.SwitchCurrentContentToSettings)
                .Discard();
        }

        private void SwitchCurrentContentToSettings(Settings? settings)
        {
            if (settings == null)
            {
                return;
            }

            this.currentContentSubscriptions.Clear();
            this.MainContent?.Dispose();
            this.MainContent = null;

            this.Content = this.Settings = new SettingsFormViewModel(this.FileName, settings, this.Kinds);

            this.Save
                .InvokeCommand(this.Settings.Save)
                .DisposeWith(this.currentContentSubscriptions);

            this.Settings.Save
                .InvokeCommand(this.UpdateSettings)
                .DisposeWith(this.currentContentSubscriptions);
        }
    }
}
