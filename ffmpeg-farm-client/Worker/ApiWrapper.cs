#define DEBUGAPI 
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FFmpegFarm.Worker.Client;
using Nito.AsyncEx.Synchronous;

namespace FFmpegFarm.Worker
{
    public class ApiWrapper : IApiWrapper
    {
        private CancellationToken _cancellationToken;
        private readonly ILogger _logger;
        public int? ThreadId { get; set; }

        private readonly StatusClient _statusClient;
        private readonly TaskClient _taskClient;
        private static readonly HttpClient HttpClient = new HttpClient{Timeout = TimeSpan.FromSeconds(10)};
        private static object GetNextTaskLock = new object();

        public ApiWrapper(string apiUri, ILogger logger, CancellationToken ct)
        {
            _taskClient = new TaskClient(HttpClient) { BaseUrl = apiUri};
            _statusClient = new StatusClient(HttpClient) { BaseUrl = apiUri };
            _logger = logger;
            _cancellationToken = ct;
        }

        ~ApiWrapper()
        {
            _logger.Debug("Disposing ApiWrapper");
        }

        public FFmpegTaskDto GetNext(string machineName)
        {
            //We lock to avoid race conditions where we could get multiple 'heavy' jobs from the server like two stereotool jobs
            lock (GetNextTaskLock) {
                return Wrap(_taskClient.GetNextAsync, machineName);
            }
        }

        public Response UpdateProgress(TaskProgressModel model, bool ignoreCancel = false)
        {
            Task<Response> stateTask;
            Response state;
                
            if (ignoreCancel)
            {
                // don't use wrapper since cancel has been called. 
                stateTask = _statusClient.UpdateProgressAsync(model, CancellationToken.None);
                stateTask.Start();
                stateTask.Wait(CancellationToken.None);
                state = stateTask.Result;
            }
            else
                state = Wrap(_statusClient.UpdateProgressAsync, model);

            return state;
        }

        /// <summary>
        /// Retries and ignores exceptions.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private TRes Wrap<TRes>(Func<TRes> func)
        {
            const int retryCount = 3;
            Exception exception = null;
            SwaggerException swaggerException = null;
            for (var x = 0; !_cancellationToken.IsCancellationRequested && x < retryCount; x++)
            {
#if DEBUGAPI
                var timer = new Stopwatch();
                timer.Start();
#endif
                try
                {
                    return func();
                }
                catch (Exception e)
                {
                    exception = e;
                    swaggerException = e as SwaggerException;
#if DEBUGAPI
                    if (swaggerException != null)
                        _logger.Warn($"{swaggerException.StatusCode} : {swaggerException.Response}", ThreadId);

                    string member = new StackFrame(2).GetMethod().Name;
                    _logger.Exception(e, ThreadId, member);
#endif
                }
#if DEBUGAPI
                finally
                {
                    _logger.Debug($"API call took {timer.ElapsedMilliseconds} ms", ThreadId);
                    timer.Stop();
                }
#endif
                var wt = Task.Delay(TimeSpan.FromSeconds(1), _cancellationToken);
                wt.WaitAndUnwrapException(_cancellationToken);
            }
            if (swaggerException != null)
                _logger.Warn($"{swaggerException.StatusCode} : {swaggerException.Response}", ThreadId);

            string caller = new StackFrame(2).GetMethod().Name;
            _logger.Exception(exception ?? new Exception(nameof(ApiWrapper)), ThreadId, caller);
            return default(TRes);
        }


        /// <summary>
        /// Retries and ignores exceptions.
        /// </summary>
        private TRes Wrap<TArg, TRes>(Func<TArg, CancellationToken, Task<TRes>> apiCall, TArg arg)
        {
            return Wrap(() => apiCall(arg, _cancellationToken).WaitAndUnwrapException(_cancellationToken));
        }
    }
}
