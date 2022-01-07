using System;
using System.Collections.Generic;

#nullable disable

namespace C3SToolboxApiTester.Models
{
    public partial class C3sApiTestlog
    {
        public int NCode { get; set; }
        public bool? Active { get; set; }
        public DateTime? LastUpdate { get; set; }
        public int? UpdateCount { get; set; }
        public DateTime? InputDate { get; set; }
        public string C3sIndicatorId { get; set; }
        public string C3sIndicatorType { get; set; }
        public string C3sRequestType { get; set; }
        public string RequestUrl { get; set; }
        public string RequestMethod { get; set; }
        public string RequestContent { get; set; }
        public int? ResponseStatus { get; set; }
        public string ResponseContent { get; set; }
        public long? ResponseDuration { get; set; }
        public string RequestLogHash { get; set; }
    }
}
