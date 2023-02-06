using k8s;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UnityKubernetesClient
{
    public class LogStream
    {
        private int maxLines = 100;
        private int currentLine = 0;

        private IKubernetes _client;
        private string _ns;
        private string _podName;
        private LogMessageEvent _logUpdate;


        public LogStream(string ns, string podName, LogMessageEvent logUpdate)
        {
            KubernetesClientConfiguration config = new KubernetesClientConfiguration();
            config.Host = "http://127.0.0.1:8080";
            config.HttpClientTimeout = TimeSpan.FromSeconds(5);
            _client = new Kubernetes(config);
            _podName = podName;
            _ns = ns;
            _logUpdate = logUpdate;
        }

        public void CreateWatch()
        {
            SafeTask.Run(async () =>
            {
                Debug.Log("Creating Logger");
                var watch = _client.CoreV1.ReadNamespacedPodLogWithHttpMessagesAsync(
                    namespaceParameter: _ns,
                    name: _podName,
                    follow: true,
                    tailLines: 10,
                    cancellationToken: CancellationToken.None
                ).ConfigureAwait(false);

                try
                {
                    var result = await watch;
                    using var sr = new StreamReader(result.Body);
                    while (!sr.EndOfStream && !SafeTask.cancellationTokenSource.IsCancellationRequested)
                    {
                        var line = await sr.ReadLineAsync();
                        _logUpdate.Invoke(line);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message);
                }
                finally
                {
                    Debug.Log("Logger Connection Closed");
                    CreateWatch();
                }
            });
        }
    }
}
