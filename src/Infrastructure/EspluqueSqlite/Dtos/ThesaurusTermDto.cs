using System;
using System.Collections.Generic;
using System.Text;

namespace EspluqueSqlite.Dtos
{
    internal class ThesaurusTermDto
    {
        public int? ConceptId { get; set; }

        public string? ReferenceName { get; set; }

        public bool IsPreferred { get; set; }

        public string Term { get; set; } = string.Empty;

        public string NormalizedTerm { get; set; } = string.Empty;
    }
}
