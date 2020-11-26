using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Enry_searchTest.Models
{
    public class DatasetModel
    {
        public string keyword { get; set; }

        public List<DataModel> dataset { get; set; }
    }
}