using System.Reactive.Disposables;

using Avalonia.ReactiveUI;

using Cineaste.Core.ViewModels.Filters;

using ReactiveUI;

namespace Cineaste.Views.Filters
{
    public partial class NoFilterInputControl : ReactiveUserControl<NoFilterInputViewModel>
    {
        public NoFilterInputControl()
        {
            this.InitializeComponent();

            this.WhenActivated(disposables =>
            {
                this.WhenAnyValue(v => v.ViewModel)
                    .BindTo(this, v => v.DataContext)
                    .DisposeWith(disposables);
            });
        }
    }
}
