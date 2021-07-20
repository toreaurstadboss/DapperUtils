using System.Collections.Generic;

namespace ToreAurstadIT.DapperUtils
{
    public class GroupingInfo<TTable>
    {

        public string Key { get; set; }

        public int TotalCount { get; set; }

        public IEnumerable<TTable> Rows { get; set; }      

    }
}