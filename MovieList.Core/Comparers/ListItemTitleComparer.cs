using System;
using System.Globalization;
using System.Linq;

using MovieList.Core.Data.Models;
using MovieList.Core.ListItems;
using MovieList.Data.Models;

namespace MovieList.Core.Comparers
{
    public sealed class ListItemTitleComparer : NullableComparerBase<ListItem>
    {
        private readonly TitleComparer titleComparer;

        public ListItemTitleComparer(CultureInfo culture, NullComparison nullComparison = NullComparison.NullsFirst)
            : base(nullComparison)
            => this.titleComparer = new TitleComparer(culture);

        protected override int CompareSafe(ListItem x, ListItem y)
            => (x, y) switch
            {
                (MovieListItem left, MovieListItem right) => this.Compare(left, right),
                (MovieListItem left, SeriesListItem right) => this.Compare(left, right),
                (MovieListItem left, FranchiseListItem right) => this.Compare(left, right),

                (SeriesListItem left, MovieListItem right) => this.Compare(left, right),
                (SeriesListItem left, SeriesListItem right) => this.Compare(left, right),
                (SeriesListItem left, FranchiseListItem right) => this.Compare(left, right),

                (FranchiseListItem left, MovieListItem right) => this.Compare(left, right),
                (FranchiseListItem left, SeriesListItem right) => this.Compare(left, right),
                (FranchiseListItem left, FranchiseListItem right) => this.Compare(left, right),

                _ => throw new NotSupportedException(
                    $"Types of list items to compare are not supported: {x.GetType()}, {y.GetType()}")
            };

        private int Compare(MovieListItem left, MovieListItem right)
            => left.Movie.Id == right.Movie.Id
                ? 0
                : this.CompareEntries(left, right, left.Movie.Entry, right.Movie.Entry);

        private int Compare(MovieListItem left, SeriesListItem right)
            => this.CompareEntries(left, right, left.Movie.Entry, right.Series.Entry);

        private int Compare(MovieListItem left, FranchiseListItem right)
            => this.Compare(right, left) * -1;

        private int Compare(SeriesListItem left, MovieListItem right)
            => this.CompareEntries(left, right, left.Series.Entry, right.Movie.Entry);

        private int Compare(SeriesListItem left, SeriesListItem right)
            => left.Series.Id == right.Series.Id
                ? 0
                : this.CompareEntries(left, right, left.Series.Entry, right.Series.Entry);

        private int Compare(SeriesListItem left, FranchiseListItem right)
            => this.Compare(right, left) * -1;

        private int Compare(FranchiseListItem left, MovieListItem right)
            => this.CompareEntries(left, right, right.Movie.Entry);

        private int Compare(FranchiseListItem left, SeriesListItem right)
            => this.CompareEntries(left, right, right.Series.Entry);

        private int Compare(FranchiseListItem left, FranchiseListItem right)
        {
            int result;

            if (left.Franchise.Id == right.Franchise.Id)
            {
                result = 0;
            } else if (left.Franchise.IsDescendantOf(right.Franchise))
            {
                result = 1;
            } else if (right.Franchise.IsDescendantOf(left.Franchise))
            {
                result = -1;
            } else if (left.Franchise.GetRootSeries().Id == right.Franchise.GetRootSeries().Id)
            {
                var (ancestor1, ancestor2) = left.Franchise.GetDistinctAncestors(right.Franchise);
                result = ancestor1.Entry?.SequenceNumber.CompareTo(ancestor2.Entry?.SequenceNumber) ?? 0;
            } else if (left.Franchise.Entry == null && right.Franchise.Entry == null)
            {
                result = this.titleComparer.Compare(
                    left.Franchise.GetListTitle()?.Name ?? String.Empty,
                    right.Franchise.GetListTitle()?.Name ?? String.Empty);

                if (result == 0)
                {
                    result = left.Franchise.GetStartYear().CompareTo(right.Franchise.GetStartYear());

                    if (result == 0)
                    {
                        result = left.Franchise.GetEndYear().CompareTo(right.Franchise.GetEndYear());
                    }
                }
            } else
            {
                return this.Compare(
                    new FranchiseListItem(left.Franchise.GetRootSeries()),
                    new FranchiseListItem(right.Franchise.GetRootSeries()));
            }

            return result;
        }

        private int CompareEntries(
            ListItem left,
            ListItem right,
            FranchiseEntry? leftEntry,
            FranchiseEntry? rightEntry)
            => (leftEntry, rightEntry) switch
            {
                (null, null) => this.CompareTitleOrYear(left, right),
                (var entry, null) => this.Compare(new FranchiseListItem(entry.ParentFranchise.GetRootSeries()), right),
                (null, var entry) => this.Compare(left, new FranchiseListItem(entry.ParentFranchise.GetRootSeries())),
                var (entry1, entry2) => entry1.ParentFranchiseId == entry2.ParentFranchiseId
                    ? entry1.SequenceNumber.CompareTo(entry2.SequenceNumber)
                    : this.CompareEntries(entry1, entry2)
            };

        private int CompareEntries(FranchiseListItem left, ListItem right, FranchiseEntry? entry)
        {
            if (entry == null)
            {
                return this.CompareTitleOrYears(new FranchiseListItem(left.Franchise.GetRootSeries()), right);
            }

            if (left.Franchise.Id == entry.ParentFranchiseId)
            {
                return -1;
            }

            if (left.Franchise.IsStrictDescendantOf(entry.ParentFranchise))
            {
                return left.Franchise.GetAllAncestors()
                        .Where(f => f.Entry != null)
                        .First(f => f.Entry!.ParentFranchiseId == entry.ParentFranchiseId)
                        .Entry!
                    .SequenceNumber
                    .CompareTo(entry.SequenceNumber);
            }

            return this.Compare(left, new FranchiseListItem(entry.ParentFranchise));
        }

        private int CompareEntries(FranchiseEntry left, FranchiseEntry right)
        {
            if (left.ParentFranchise.IsStrictDescendantOf(right.ParentFranchise))
            {
                return left.ParentFranchise.GetAllAncestors()
                        .Where(a => a.Entry != null)
                        .First(a => a.Entry!.ParentFranchiseId == right.ParentFranchiseId)
                        .Entry!
                    .SequenceNumber
                    .CompareTo(right.SequenceNumber);
            }

            if (right.ParentFranchise.IsStrictDescendantOf(left.ParentFranchise))
            {
                return left.SequenceNumber.CompareTo(
                    right.ParentFranchise.GetAllAncestors()
                        .Where(a => a.Entry != null)
                        .First(a => a.Entry!.ParentFranchiseId == left.ParentFranchiseId)
                        .Entry!
                        .SequenceNumber);
            }

            return this.Compare(
                new FranchiseListItem(left.ParentFranchise),
                new FranchiseListItem(right.ParentFranchise));
        }

        private int CompareTitleOrYear(ListItem left, ListItem right)
        {
            int result = this.titleComparer.Compare(left.Title, right.Title);
            return result != 0 ? result : left.Year.CompareTo(right.Year);
        }

        private int CompareTitleOrYears(FranchiseListItem left, ListItem right)
        {
            int result = this.titleComparer.Compare(
                left.Franchise.GetListTitle()?.Name ?? String.Empty, right.Title);

            if (result != 0)
            {
                return result;
            }

            result = left.StartYearToCompare.CompareTo(right.StartYearToCompare);

            return result != 0 ? result : left.EndYearToCompare.CompareTo(right.EndYearToCompare);
        }
    }
}
