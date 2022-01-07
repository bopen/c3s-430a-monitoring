using System;
using System.Collections.Generic;

#nullable disable

namespace C3SToolboxApiTester.Models
{
    public partial class C3sApiMetadatum
    {
        public int NCode { get; set; }
        public bool? Active { get; set; }
        public DateTime? LastUpdate { get; set; }
        public int? UpdateCount { get; set; }
        public DateTime? InputDate { get; set; }
        public string C3sIdentifier { get; set; }
        public string EcdeUrl { get; set; }
        public string Title { get; set; }
    }
}
