using Dapper.Contrib.Extensions;

namespace Cineaste.Data.Models
{
    [Table("MovieTags")]
    internal sealed class MovieTag : EntityBase
    {
        public int MovieId { get; set; }
        public int TagId { get; set; }
    }
}
