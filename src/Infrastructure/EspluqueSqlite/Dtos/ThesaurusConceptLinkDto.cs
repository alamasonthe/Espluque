using System;
using System.Collections.Generic;
using System.Text;

namespace EspluqueSqlite.Dtos
{
    internal class ThesaurusConceptLinkDto
    {
        public int ParentConceptId { get; set; }

        public int ChildConceptId { get; set; }
    }
}
