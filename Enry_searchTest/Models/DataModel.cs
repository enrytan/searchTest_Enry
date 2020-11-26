using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Enry_searchTest.Models
{
    public class DataModel
    {
        public Guid stringId { get; set; }
        public string stringContent { get; set; }
        public int matchCount { get; set; }

    }
}