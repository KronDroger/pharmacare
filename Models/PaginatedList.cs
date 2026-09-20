using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace CarePlusPharmacy.Models
{
    public interface IPaginationInfo
    {
        int PageIndex { get; }
        int PageSize { get; }
        int TotalCount { get; }
        int TotalPages { get; }
        bool HasPreviousPage { get; }
        bool HasNextPage { get; }
    }

    public class PaginatedList<T> : List<T>, IPaginationInfo
    {
        public int PageIndex { get; private set; }
        public int PageSize { get; private set; }
        public int TotalCount { get; private set; }
        public int TotalPages { get; private set; }

        public List<T> Items => this;

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            TotalCount = count;
            PageSize = pageSize;
            TotalPages = count == 0 ? 1 : (int)Math.Ceiling(count / (double)pageSize);
            PageIndex = Math.Max(1, Math.Min(pageIndex, TotalPages));

            AddRange(items);
        }

        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
        {
            // Allowed page sizes
            int[] allowedSizes = { 10, 25, 50, 100 };
            if (!allowedSizes.Contains(pageSize))
            {
                pageSize = 10;
            }

            int count = await source.CountAsync();
            int totalPages = count == 0 ? 1 : (int)Math.Ceiling(count / (double)pageSize);

            if (pageIndex < 1) pageIndex = 1;
            if (pageIndex > totalPages) pageIndex = totalPages;

            int skip = (pageIndex - 1) * pageSize;
            if (skip < 0) skip = 0;

            var items = await source.Skip(skip).Take(pageSize).ToListAsync();
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }

        public static PaginatedList<T> Create(IEnumerable<T> source, int pageIndex, int pageSize)
        {
            int[] allowedSizes = { 10, 25, 50, 100 };
            if (!allowedSizes.Contains(pageSize))
            {
                pageSize = 10;
            }

            var list = source.ToList();
            int count = list.Count;
            int totalPages = count == 0 ? 1 : (int)Math.Ceiling(count / (double)pageSize);

            if (pageIndex < 1) pageIndex = 1;
            if (pageIndex > totalPages) pageIndex = totalPages;

            int skip = (pageIndex - 1) * pageSize;
            if (skip < 0) skip = 0;

            var items = list.Skip(skip).Take(pageSize).ToList();
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }
}
