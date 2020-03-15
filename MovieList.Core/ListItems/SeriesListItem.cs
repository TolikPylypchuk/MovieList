using System;

using MovieList.Data.Models;

namespace MovieList.ListItems
{
    public class SeriesListItem : ListItem
    {
        public SeriesListItem(Series series)
            : base(
                  $"S-{series.Id}",
                  series.Entry,
                  series.Title.Name,
                  series.OriginalTitle.Name,
                  series.Seasons.Count != 0 || series.SpecialEpisodes.Count != 0
                    ? series.StartYear != series.EndYear
                        ? $"{series.StartYear} - {series.EndYear}"
                        : series.StartYear.ToString()
                    : String.Empty,
                  series.StartYear,
                  series.EndYear,
                  series.GetActiveColor())
            => this.Series = series;

        public Series Series { get; }

        public override MovieSeriesEntry? Entry
            => this.Series.Entry;
    }
}
