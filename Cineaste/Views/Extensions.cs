using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;

using Avalonia.Controls;
using Avalonia.Media.Imaging;

using Cineaste.Core;

using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Extensions;

using static Cineaste.Core.ServiceUtil;

namespace Cineaste.Views
{
    public static class Extensions
    {
        private static readonly MemberInfo TextProperty =
            typeof(TextBlock).GetProperty(nameof(TextBlock.Text))!;

        public static IDisposable BindDefaultValidation<TView, TViewModel, T>(
            this TView view,
            TViewModel? viewModel,
            Expression<Func<TViewModel, T?>> prop,
            Expression<Func<TView, TextBlock>> errorTextBlock)
            where TView : class, IViewFor<TViewModel>
            where TViewModel : class, IReactiveObject, IValidatableViewModel
        {
            var subscriptions = new CompositeDisposable();

            view.BindValidation(viewModel, prop, errorTextBlock.WithProperty<TView, TextBlock, string>(TextProperty))
                .DisposeWith(subscriptions);

            view.WhenAnyValue(prop.PrependProperty<TViewModel, TView, T>(nameof(IViewFor.ViewModel)))
                .Take(1)
                .Select(_ => String.Empty)
                .BindTo(view, errorTextBlock.WithProperty<TView, TextBlock, string?>(TextProperty))
                .DisposeWith(subscriptions);

            return subscriptions;
        }

        public static void SetEnumValues<TEnum>(this ComboBox comboBox, params TEnum[] values)
            where TEnum : struct, Enum
        {
            var converter = GetDefaultService<IEnumConverter<TEnum>>();

            comboBox.Items = (values.Length != 0 ? values : Enum.GetValues<TEnum>())
                .Select(converter.ToString)
                .ToList();
        }

        public static IBitmap? AsImage([AllowNull] this byte[] imageData) =>
            imageData == null || imageData.Length == 0 ? null : new Bitmap(new MemoryStream(imageData));

        private static Expression<Func<TParam, TResult>> WithProperty<TParam, TType, TResult>(
            this Expression<Func<TParam, TType>> expr,
            MemberInfo prop)
        {
            var body = Expression.MakeMemberAccess(expr.Body, prop);
            return Expression.Lambda<Func<TParam, TResult>>(body, expr.Parameters);
        }

        private static Expression<Func<TNewParam, T?>> PrependProperty<TParam, TNewParam, T>(
            this Expression<Func<TParam, T?>> originalExpression,
            string prop)
        {
            var param = Expression.Parameter(typeof(TNewParam));

            var newProperty = Expression.MakeMemberAccess(param, typeof(TNewParam).GetProperty(prop)!);

            var body = originalExpression.Body
                .GetExpressionChain()
                .OfType<MemberExpression>()
                .Reverse()
                .Aggregate(newProperty, (parent, expr) => Expression.MakeMemberAccess(parent, expr.Member));

            return Expression.Lambda<Func<TNewParam, T?>>(body, param);
        }
    }
}
