using System;
using System.Collections.Generic;

using Dapper.Contrib.Extensions;

namespace MovieList.Data.Models
{
    [Table("MovieSeriesEntries")]
    public sealed class MovieSeriesEntry : EntityBase
    {
        public int? MovieId { get; set; }
        public Movie? Movie { get; set; }

        public int? SeriesId { get; set; }
        public Series? Series { get; set; }

        public int? MovieSeriesId { get; set; }
        public MovieSeries? MovieSeries { get; set; }

        public int ParentSeriesId { get; set; }
        public MovieSeries ParentSeries { get; set; }

        public int SequenceNumber { get; set; }
        public int? DisplayNumber { get; set; }

        [Computed]
        public IList<Title> Titles
            => (this.Movie, this.Series, this.MovieSeries) switch
            {
                (var movie, null, null) when movie != null => movie.Titles,
                (null, var series, null) when series != null => series.Titles,
                (null, null, var movieSeries) when movieSeries != null => movieSeries.ActualTitles,
                _ => throw new InvalidOperationException("At least one movie series entry component must be non-null.")
            };

        public override string ToString()
            => $"Movie Series Entry #{this.Id}: {this.Titles}";
    }
}