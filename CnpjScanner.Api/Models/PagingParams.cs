using System;

namespace CnpjScanner.Api.Models;

public class PagingParams
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
