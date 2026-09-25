namespace BabaTune.Domain.Common
{
	public class PagedResult<T>
	{
		public IReadOnlyCollection<T> Items { get; set; } = [];
		public int TotalCount { get; set; }
		public int PageNumber { get; set; }
		public int PageSize { get; set; }
		public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
		public bool HasNextPage => PageNumber < TotalPages;
		public bool HasPreviousPage => PageNumber > 1;
	}
}
