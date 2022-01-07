using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Maris;
using C3SToolbox;
using C3SToolboxApiTester.Models;

using System.Text.RegularExpressions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
//using System.Globalization;

namespace C3SToolboxApiTester
{
    class Program
    {

        int amountOfParameterCombinationsToGenerate = 1;
        int timeToSleepBetweenPoll = 10 * 1000; // = 10 seconds

        int concurrentWorkflowConfigDownloads = 6;
        int concurrentWorkflowDownloads = 6;


        private RequestHandler requestHandler;

        private string githubDataUrl = "https://raw.githubusercontent.com/maris-development/c3s-434-portal/static-generator/data/data_consolidated.json";

        private const string c3sBaseUrl = "https://cds.climate.copernicus.eu";

        private api_testerContext DbContext;

        private ConsolidatedData ECDEConfig;
        static void Main(string[] args)
        {
            new Program().Start();
        }

        public void Start()
        {
            AppLib.ProgramName = "c3s_api_tester";

            AppLib.Init();

            requestHandler = new RequestHandler();
            requestHandler.debug = true;

            // Haal ECDE config op van github
            GetECDEConfig();

            // Aan de hand van de indicators in de ECDE config:
            // test 1: Retrieve Workflow Configs
            RetrieveC3SWorkflowConfigs();

            // Generate parameter combtionations
            GenerateC3SWorkflowParameterCombinations();

            // test 2: Retrieve Workflow Combination
            RetrieveWorkflows();


            AppLib.End();
        }



        private void GetECDEConfig()
        {
            try
            {
                // Download .json
                HttpRequestReturnValue response = requestHandler.GetRequest(githubDataUrl);

                if (response.StatusCode == 200)
                {
                    //  Deserialize naar een ConsolidatedData object.
                    ECDEConfig = JsonConvert.DeserializeObject<ConsolidatedData>(response.Content);

                   
                }
                else
                {
                    // Die als we het niet kunnen vinden
                    AppLib.AppendLog(response.Content);
                    AppLib.Kill();
                }
            }
            catch (System.Exception e)
            {
                AppLib.AppendLog(e);
            }

        }

        //dictionary om workflow configs op te slaan
        // Bevat: Identifier, Type (overview of detail), WorkflowConfig (C3S json) en IndicatorConfig (github json)
        private Dictionary<string, (string Identifier, string Type, WorkflowConfig WorkflowConfig, IndicatorConfig IndicatorConfig)>
            C3SWorkflowConfigs = new Dictionary<string, (string Identifier, string Type, WorkflowConfig WorkflowConfig, IndicatorConfig IndicatorConfig)>();

        // Haalt alle WorkflowConfigs op bij de inidicators.
        private void RetrieveC3SWorkflowConfigs()
        {
            try
            {
                //lijst om config URLs bij te houden
                // Record bevat: Identifier, Type, COnfigUrl, IndicatorConfig
                List<(string Identifier, string Type, string ConfigUrl, IndicatorConfig IndicatorConfig)> C3SConfigUrls =
                    new List<(string Identifier, string Type, string ConfigUrl, IndicatorConfig IndicatorConfig)>();

                //Per inidicator zijn er 2 types: overview & detail, voeg ze beide toe aan de lijst van URLs
                foreach (var entry in ECDEConfig.indicators)
                {
                    string c3sIdentifier = entry.Key;
                    IndicatorConfig indicator = entry.Value;

                    var dbIndicatorObject = new C3sApiMetadatum();

                    using (var context = new api_testerContext())
                    {
                        var queryableContext = context.C3sApiMetadata.AsQueryable();

                        var result = queryableContext.SingleOrDefault(ind => ind.C3sIdentifier == c3sIdentifier);

                        if(null != result)
                        {
                            dbIndicatorObject = result;
                        }

                        dbIndicatorObject.C3sIdentifier = indicator.identifier;

                        //string urlSafeTitle = indicator.page_title;

                        //urlSafeTitle = Regex.Replace(urlSafeTitle, "[^a-zA-Z0-9]", " ");
                        //urlSafeTitle = Regex.Replace(urlSafeTitle, "\\s+", "-");

                        //dbIndicatorObject.EcdeUrl = "https://climate-adapt.eea.europa.eu/metadata/indicators/" + urlSafeTitle.ToLower();

                        dbIndicatorObject.Title = indicator.page_title;

                        if (null == result)
                        {
                            context.C3sApiMetadata.Add(dbIndicatorObject);
                        }

                        context.SaveChanges();

                    }

                    C3SConfigUrls.Add((c3sIdentifier, "overview", indicator.overview, indicator));
                    C3SConfigUrls.Add((c3sIdentifier, "detail", indicator.detail, indicator));
                }

                // Soorteer op Identifier (e.g. 'C3S_434_001')
                C3SConfigUrls.Sort((a, b) => a.Identifier.CompareTo(b.Identifier));


                // Download meerdere workflow configs tegelijk
                var listOfTasks = new List<Task>();

                foreach (var entry in C3SConfigUrls)
                {
                    listOfTasks.Add(new Task(
                       () => RetrieveC3SWorkflowConfig(entry)
                    ));
                }

                //max 4 workflows tegelijk downloaden.
                StartAndWaitAllThrottled(listOfTasks, concurrentWorkflowConfigDownloads);
            }
            catch (System.Exception e)
            {
                AppLib.AppendLog(e);
            }
        }

        private void RetrieveC3SWorkflowConfig((string Identifier, string Type, string ConfigUrl, IndicatorConfig IndicatorConfig) entry)
        {
            //AppLib.AppendLog(string.Join(",", entry));




            HttpRequestReturnValue response = null;

            using (var context = new api_testerContext())
            {

                var C3sApiTestlogs = context.C3sApiTestlogs.AsQueryable();

                var other = (
                    from l in C3sApiTestlogs
                    where (l.C3sIndicatorId == entry.Identifier) &&
                    (l.C3sIndicatorType == entry.Type) &&
                    (l.C3sRequestType == "workflow_config") &&
                    (l.RequestLogHash != "from_cache") &&
                    (l.InputDate >= DateTime.Today)
                    orderby l.InputDate descending
                    select l
                );

                if (other.Count() > 0)
                {
                    string config = other.First().ResponseContent;
                    response = new HttpRequestReturnValue(1200, config, "from_cache");
                }
            }

            //start stopwatch
            Stopwatch watch = Stopwatch.StartNew();

            // voer request uit
            if (null == response)
            {
                response = requestHandler.GetRequest(entry.ConfigUrl);
            }

            //stop stopwatch
            watch.Stop();

            //creer log entry
            var log = new C3sApiTestlog();

            log.C3sIndicatorType = entry.Type;
            log.C3sIndicatorId = entry.Identifier;
            log.C3sRequestType = "workflow_config";

            log.RequestUrl = entry.ConfigUrl;
            log.RequestMethod = "GET";
            log.RequestLogHash = response.RequestHash;

            log.ResponseDuration = watch.ElapsedMilliseconds;
            log.ResponseStatus = response.StatusCode;
            log.ResponseContent = response.Content;


            //opslaan in db
            SaveLogToDb(log);

            //handel verder af voor dit programma.
            switch (response.StatusCode)
            {
                case 200:
                case 1200: //vanuit cache.
                    try
                    {
                        // Deserializeer JSON naar WorkflowCOnfig
                        WorkflowConfig workflow = JsonConvert.DeserializeObject<WorkflowConfig>(response.Content);

                        // Voeg toe aan array (lock om threadsafe te maken)
                        lock (C3SWorkflowConfigs)
                        {
                            C3SWorkflowConfigs[entry.Identifier + "_" + entry.Type] = (entry.Identifier, entry.Type, workflow, entry.IndicatorConfig);
                        }
                    }
                    catch (Newtonsoft.Json.JsonReaderException e)
                    {
                        AppLib.AppendLog(e);
                    }
                    break;
                default:
                    AppLib.AppendLog(response.Content, response.StatusCode.ToString());
                    break;
            }
        }


        //Dict om workflowparameter combinaties op te slaan 
        private List<(string Identifier, string Type, Dictionary<string, string> Parameters)> C3SWorkflowParameters =
                new List<(string Identifier, string Type, Dictionary<string, string> Parameters)>();

        private void GenerateC3SWorkflowParameterCombinations()
        {
            try
            {

                foreach (var kvp in C3SWorkflowConfigs)
                {
                    var entry = kvp.Value;

                    Dictionary<string, List<string>> possibleParameters = new Dictionary<string, List<string>>();

                    //per parameter
                    foreach (var param in entry.WorkflowConfig.@params)
                    {
                        //vul een lijst van opties bij deze parameter
                        List<string> values = new List<string>();

                        foreach (var value in param.props.values)
                        {
                            values.Add(value.value);
                        }

                        possibleParameters[param.props.name] = values;
                    }

                    // genereer combinaties van deze parameters:
                    for (int i = 0; i < amountOfParameterCombinationsToGenerate; i++)
                    {
                        // dict om paramterCombinatie op te slaan met a: x, b: y manier
                        Dictionary<string, string> parameterCombination = new Dictionary<string, string>();

                        //kies willekeurige waarde per parameter
                        foreach (var parameterEntry in possibleParameters)
                        {
                            int randomIndex = AppLib.rnd.Next(parameterEntry.Value.Count);
                            parameterCombination[parameterEntry.Key] = parameterEntry.Value[randomIndex];
                        }

                        //probeer extra variabelen uit de InidicatorConfig te halen, zoals suitability bij de mosquito indicator
                        Dictionary<string, string> extraVars = null;
                        switch (entry.Type)
                        {
                            case "overview":
                                extraVars = entry.IndicatorConfig.vars.overview;
                                break;
                            case "detail":
                                extraVars = entry.IndicatorConfig.vars.detail;
                                break;
                        }
                        //als er extra vars zijn, voeg ze toe/schrijf gegenereerde over
                        if (null != extraVars)
                        {
                            foreach (var extraVar in extraVars)
                            {
                                parameterCombination[extraVar.Key] = extraVar.Value;
                            }
                        }

                        //sla op voor volgende routine
                        C3SWorkflowParameters.Add((entry.Identifier, entry.Type, parameterCombination));
                    }

                }

            }
            catch (System.Exception e)
            {
                AppLib.AppendLog(e);
            }
        }

        private void RetrieveWorkflows()
        {
            try
            {
                //maak taaklijst aan
                var listOfTasks = new List<Task>();

                //voeg elke nieuwe task toe
                foreach (var entry in C3SWorkflowParameters)
                {
                    var config = C3SWorkflowConfigs[entry.Identifier + "_" + entry.Type].WorkflowConfig;
                    var parameters = entry.Parameters;

                    listOfTasks.Add(new Task(
                        () => RequestWorkflow(config, parameters, (entry.Identifier, entry.Type))
                    ));
                }

                //Voer alle taken uit, 3 tegelijk.
                StartAndWaitAllThrottled(listOfTasks, concurrentWorkflowDownloads);
            }
            catch (System.Exception e)
            {
                AppLib.AppendLog(e);
            }
        }

        private int workflowRequestCounter = 0;

        private void RequestWorkflow(WorkflowConfig config, Dictionary<string, string> parameters, (string Identifier, string Type) C3SMetadata)
        {
            workflowRequestCounter++;

            string name = "#" + workflowRequestCounter;

            //stel URL samen
            string url = c3sBaseUrl + config.workflow_url + '/' + config.workflow_entry_point;
            string content = JsonConvert.SerializeObject(parameters); //serialize content

            //start stopwatch
            Stopwatch watch = Stopwatch.StartNew();

            // voer request uit
            HttpRequestReturnValue response = ToolboxRequest202TillDone(url, content, "POST", watch, name: name);

            //stop stopwatch
            watch.Stop();

            //creer log entry
            var log = new C3sApiTestlog();

            log.C3sIndicatorType = C3SMetadata.Type;
            log.C3sIndicatorId = C3SMetadata.Identifier;
            log.C3sRequestType = "workflow_execution";

            log.RequestUrl = url;
            log.RequestMethod = "POST";
            log.RequestContent = content;
            log.RequestLogHash = response.RequestHash;

            log.ResponseDuration = watch.ElapsedMilliseconds;
            log.ResponseStatus = response.StatusCode;
            log.ResponseContent = response.Content;


            //save to DB
            SaveLogToDb(log);

            //handel verder af voor dit programma.
            var responseObj = JsonConvert.DeserializeObject<ToolboxWorkflowResponse>(response.Content);

            AppLib.AppendLog(response.StatusCode, response.RequestHash);
        }

        private HttpRequestReturnValue ToolboxRequest202TillDone(string url, string content = "", string type = "GET", Stopwatch watch = null, string requestHash = null, string name = "")
        {

            if (watch == null)
            {
                watch = Stopwatch.StartNew();
            }

            HttpRequestReturnValue response;

            switch (type)
            {
                default:
                case "GET":
                    response = requestHandler.GetRequest(url, requestHash);
                    break;

                case "POST":
                    response = requestHandler.PostRequest(url, content, requestHash: requestHash);
                    break;
            }

            switch (response.StatusCode)
            {
                case 202:
                    var responseObj = JsonConvert.DeserializeObject<ToolboxWorkflowResponse>(response.Content);

                    AppLib.AppendLog(watch.Elapsed.ToString(@"mm\:ss\.fff") + " " + name, response.RequestHash);


                    // Check if timeout has happened, to prevent indicators from taking hours to load
                    if (watch.ElapsedMilliseconds > requestHandler.TimeoutLong)
                    {
                        response.StatusCode = 1408;
                        break;
                    }

                    // Poll queue again after 1.5 seconds
                    Thread.Sleep(timeToSleepBetweenPoll);
                    return ToolboxRequest202TillDone(c3sBaseUrl + responseObj.queue, type: "GET", watch: watch, requestHash: response.RequestHash, name: name);
            }

            return response;
        }

        private void SaveLogToDb(C3sApiTestlog log)
        {
            using (var context = new api_testerContext())
            {
                //save to DB
                context.C3sApiTestlogs.Add(log);
                context.SaveChanges();
            }
        }

        private void Test()
        {
            HttpRequestReturnValue response = requestHandler.PostRequest("http://httpbin.org/post?whoo=hoo", "{\"test\": \"case\"}");

            AppLib.AppendLog(response.StatusCode.ToString());
            AppLib.AppendLog(response.Content);
        }

        /// <summary>
        /// Starts the given tasks and waits for them to complete. This will run, at most, the specified number of tasks in parallel.
        /// <para>NOTE: If one of the given tasks has already been started, an exception will be thrown.</para>
        /// </summary>
        /// <param name="tasksToRun">The tasks to run.</param>
        /// <param name="maxActionsToRunInParallel">The maximum number of tasks to run in parallel.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static void StartAndWaitAllThrottled(IEnumerable<Task> tasksToRun, int maxActionsToRunInParallel, CancellationToken cancellationToken = new CancellationToken())
        {
            StartAndWaitAllThrottled(tasksToRun, maxActionsToRunInParallel, -1, cancellationToken);
        }

        /// <summary>
        /// Starts the given tasks and waits for them to complete. This will run the specified number of tasks in parallel.
        /// <para>NOTE: If a timeout is reached before the Task completes, another Task may be started, potentially running more than the specified maximum allowed.</para>
        /// <para>NOTE: If one of the given tasks has already been started, an exception will be thrown.</para>
        /// </summary>
        /// <param name="tasksToRun">The tasks to run.</param>
        /// <param name="maxActionsToRunInParallel">The maximum number of tasks to run in parallel.</param>
        /// <param name="timeoutInMilliseconds">The maximum milliseconds we should allow the max tasks to run in parallel before allowing another task to start. Specify -1 to wait indefinitely.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public static void StartAndWaitAllThrottled(IEnumerable<Task> tasksToRun, int maxActionsToRunInParallel, int timeoutInMilliseconds, CancellationToken cancellationToken = new CancellationToken())
        {
            // Convert to a list of tasks so that we don't enumerate over it multiple times needlessly.
            var tasks = (List<Task>)tasksToRun;

            using (var throttler = new SemaphoreSlim(maxActionsToRunInParallel))
            {
                var postTaskTasks = new List<Task>();

                // Have each task notify the throttler when it completes so that it decrements the number of tasks currently running.
                tasks.ForEach(t => postTaskTasks.Add(t.ContinueWith(tsk => throttler.Release())));

                // Start running each task.
                foreach (var task in tasks)
                {
                    // Increment the number of tasks currently running and wait if too many are running.
                    throttler.Wait(timeoutInMilliseconds, cancellationToken);

                    cancellationToken.ThrowIfCancellationRequested();
                    task.Start();
                }

                // Wait for all of the provided tasks to complete.
                // We wait on the list of "post" tasks instead of the original tasks, otherwise there is a potential race condition where the throttler&amp;amp;#39;s using block is exited before some Tasks have had their "post" action completed, which references the throttler, resulting in an exception due to accessing a disposed object.
                Task.WaitAll(postTaskTasks.ToArray(), cancellationToken);
            }
        }
    }

}
