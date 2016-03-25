namespace PetaPoco.Internal
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;

    internal static class PagingHelper
    {
        public static Regex rxColumns = new Regex(@"\A\s*SELECT\s+((?:\((?>\((?<depth>)|\)(?<-depth>)|.?)*(?(depth)(?!))\)|.)*?)(?<!,\s+)\bFROM\b", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        public static Regex rxDistinct = new Regex(@"\ADISTINCT\s", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);
        public static Regex rxOrderBy = new Regex(@"\bORDER\s+BY\s+(?:\((?>\((?<depth>)|\)(?<-depth>)|.?)*(?(depth)(?!))\)|[\w\(\)\.])+(?:\s+(?:ASC|DESC))?(?:\s*,\s*(?:\((?>\((?<depth>)|\)(?<-depth>)|.?)*(?(depth)(?!))\)|[\w\(\)\.])+(?:\s+(?:ASC|DESC))?)*", RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

        public static bool SplitSQL(string sql, out SQLParts parts)
        {
            parts.sql = sql;
            parts.sqlSelectRemoved = null;
            parts.sqlCount = null;
            parts.sqlOrderBy = null;
            Match match = rxColumns.Match(sql);
            if (!match.Success)
            {
                return false;
            }
            Group group = match.Groups[1];
            parts.sqlSelectRemoved = sql.Substring(group.Index);
            if (rxDistinct.IsMatch(parts.sqlSelectRemoved))
            {
                parts.sqlCount = sql.Substring(0, group.Index) + "COUNT(" + match.Groups[1].ToString().Trim() + ") " + sql.Substring(group.Index + group.Length);
            }
            else
            {
                parts.sqlCount = sql.Substring(0, group.Index) + "COUNT(*) " + sql.Substring(group.Index + group.Length);
            }
            match = rxOrderBy.Match(parts.sqlCount);
            if (!match.Success)
            {
                parts.sqlOrderBy = null;
            }
            else
            {
                group = match.Groups[0];
                parts.sqlOrderBy = group.ToString();
                parts.sqlCount = parts.sqlCount.Substring(0, group.Index) + parts.sqlCount.Substring(group.Index + group.Length);
            }
            return true;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SQLParts
        {
            public string sql;
            public string sqlCount;
            public string sqlSelectRemoved;
            public string sqlOrderBy;
        }
    }
}

