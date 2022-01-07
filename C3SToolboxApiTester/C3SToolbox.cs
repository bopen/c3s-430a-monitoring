using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace C3SToolbox
{
    class C3SToolbox
    {
    }

    public class JsonObject
    {
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
    public class WorkflowConfig : JsonObject
    {
        public string workflow_url { get; set; }
        public string workflow_entry_point { get; set; }
        public string sdk_version { get; set; }
        public string configuration_version { get; set; }
        public string name { get; set; }
        //public JObject layout { get; set; }
        public WorkflowParameter[] @params { get; set; }


    }

    public class WorkflowParameter : JsonObject
    {
        public string type { get; set; }
        public string uid { get; set; }
        public WorkflowParameterProps props { get; set; }
       

    }
    public class WorkflowParameterProps : JsonObject
    {
        public string name { get; set; }
        public string id { get; set; }
        public string label { get; set; }
        public ValueLabelPair[] values { get; set; }
       

    }

    public class ValueLabelPair : JsonObject
    {

        public string value { get; set; }
        public string label { get; set; }
    }

    public class ToolboxWorkflowResponse : JsonObject
    {
        public ToolboxWorkflowResponseMetaData metadata { get; set; }
        public string queue { get; set; }
        public JArray results { get; set; }
      

    }

    public class ToolboxWorkflowResponseMetaData : JsonObject
    {
        public JArray logging { get; set; }
        public string message { get; set; }
        public string status { get; set; }
        public string timestamp { get; set; }
      
    }

    // Class om https://raw.githubusercontent.com/maris-development/c3s-434-portal/static-generator/data/data_consolidated.json te omschrijven
    public class ConsolidatedData : JsonObject
    {
        public Dictionary<string, IndicatorConfig> indicators { get; set; }
        public string toolbox_embed_version { get; set; }

    }

    public class IndicatorConfig : JsonObject
    {
        public string indicator_title { get; set; }
        public string page_title { get; set; }
        public string detail { get; set; }
        public string overview { get; set; }
        public string identifier { get; set; }
        public IndicatorConfigVars vars { get; set; }
    }

    public class IndicatorConfigVars : JsonObject
    {
        public Dictionary<string, string> detail { get; set; }
        public Dictionary<string, string> overview { get; set; }
    }

}
