using k8s;
using k8s.Autorest;
using k8s.Models;
using System.Collections.Concurrent;
using UnityEngine;

namespace UnityKubernetesClient
{
    public class ClusterState
    {
        public ConcurrentDictionary<String, V1Pod> Pods { get; set; }
        public ConcurrentDictionary<String, V1ReplicaSet> ReplicaSets { get; set; }
        public ConcurrentDictionary<String, V1Deployment> Deployments { get; set; }
        public ConcurrentDictionary<String, V1StatefulSet> StatefulSets { get; set; }
        public ConcurrentDictionary<String, V1DaemonSet> DaemonSets { get; set; }
        public ConcurrentDictionary<String, V1Service> Services { get; set; }
        public ConcurrentDictionary<String, V1Ingress> Ingresses { get; set; }
        public ConcurrentDictionary<String, V1PersistentVolume> PersistentVolumes { get; set; }
        public ConcurrentDictionary<String, V1PersistentVolumeClaim> PersistentVolumeClaims { get; set; }

        private IKubernetes _client;
        private readonly IKubernetes _podClient;
        private readonly IKubernetes _replicaSetClient;
        private readonly IKubernetes _deploymentClient;
        private readonly IKubernetes _statefulSetClient;
        private readonly IKubernetes _daemonSetClient;
        private readonly IKubernetes _serviceClient;
        private readonly IKubernetes _ingressClient;
        private readonly IKubernetes _persistentVolumeClient;
        private readonly IKubernetes _persistentVolumeClaimClient;

        public ClusterState() {
            KubernetesClientConfiguration config = new KubernetesClientConfiguration();
            config.Host = "http://127.0.0.1:8080";
            config.HttpClientTimeout = TimeSpan.FromSeconds(5);
            _client = new Kubernetes(config);
            _podClient = new Kubernetes(config);
            _replicaSetClient = new Kubernetes(config);
            _deploymentClient = new Kubernetes(config);
            _statefulSetClient = new Kubernetes(config);
            _daemonSetClient = new Kubernetes(config);
            _serviceClient = new Kubernetes(config);
            _ingressClient = new Kubernetes(config);
            _persistentVolumeClient = new Kubernetes(config);
            _persistentVolumeClaimClient = new Kubernetes(config);
        }

        public void startWatches()
        {
            WatchPods();
            WatchReplicaSets();
            WatchDeployments();
            WatchStatefulSets();
            WatchDaemonSets();
            WatchServices();
            WatchIngresses();
            WatchPersistentVolumes();
            WatchPersistentVolumeClaims();
        }

        void CreateWatch<T, TList>(Task<HttpOperationResponse<TList>> watch, ConcurrentDictionary<string, T> dictionary, Action resetCallback)
       where T :
       IKubernetesObject,
       IKubernetesObject<V1ObjectMeta>,
       IMetadata<V1ObjectMeta>,
       IValidate
        {
            SafeTask.Run(() =>
            {
                Debug.Log($"Watcher: Starting: {typeof(T).Name}");
                var resetEvent = new ManualResetEvent(false);
                using var watcher = watch.Watch<T, TList>(
                    (t, resource) =>
                    {
                        Debug.Log($"Watcher: {typeof(T).Name} event: {resource.Namespace()}/{resource.Name()} {t}");
                        if (t == WatchEventType.Deleted)
                        {
                            dictionary.TryRemove($"{resource.Namespace()}/{resource.Name()}", out T removed);
                        }
                        else
                        {
                            dictionary.AddOrUpdate($"{resource.Namespace()}/{resource.Name()}", resource,
                                (key, old) => resource);
                        }
                    },
                    e =>
                    {
                        Debug.Log($"Watcher: Error: {typeof(T).Name}");
                        Debug.Log(e.ToString());
                    },
                    () =>
                    {
                        Debug.Log($"Watcher: Connection Closed: {typeof(T).Name}");
                        resetCallback();
                        resetEvent.Set();
                    });
                resetEvent.WaitOne();
            });
        }

        void WatchPods()
        {
            Pods ??= new ConcurrentDictionary<string, V1Pod>();
            var watch = _podClient.CoreV1.ListPodForAllNamespacesWithHttpMessagesAsync(
                watch: true,
                timeoutSeconds: 60000,
                cancellationToken: SafeTask.cancellationTokenSource.Token
            );
            CreateWatch(watch, Pods, WatchPods);
        }

        void WatchReplicaSets()
        {
            ReplicaSets ??= new ConcurrentDictionary<string, V1ReplicaSet>();
            var watch = _replicaSetClient.AppsV1.ListReplicaSetForAllNamespacesWithHttpMessagesAsync(
                watch: true,
                timeoutSeconds: 60000,
                cancellationToken: SafeTask.cancellationTokenSource.Token

            );
            CreateWatch(watch, ReplicaSets, WatchReplicaSets);
        }

        void WatchDeployments()
        {
            Deployments ??= new ConcurrentDictionary<string, V1Deployment>();
            var watch = _deploymentClient.AppsV1.ListDeploymentForAllNamespacesWithHttpMessagesAsync(
                watch: true,
                timeoutSeconds: 60000,
                cancellationToken: SafeTask.cancellationTokenSource.Token
            );
            CreateWatch(watch, Deployments, WatchDeployments);
        }

        void WatchStatefulSets()
        {
            StatefulSets ??= new ConcurrentDictionary<string, V1StatefulSet>();
            var watch = _statefulSetClient.AppsV1.ListStatefulSetForAllNamespacesWithHttpMessagesAsync(
                watch: true,
                timeoutSeconds: 60000,
                cancellationToken: SafeTask.cancellationTokenSource.Token
            );
            CreateWatch(watch, StatefulSets, WatchStatefulSets);
        }

        void WatchDaemonSets()
        {
            DaemonSets ??= new ConcurrentDictionary<string, V1DaemonSet>();
            var watch = _daemonSetClient.AppsV1.ListDaemonSetForAllNamespacesWithHttpMessagesAsync(
                watch: true,
                timeoutSeconds: 60000,
                cancellationToken: SafeTask.cancellationTokenSource.Token
            );
            CreateWatch(watch, DaemonSets, WatchDaemonSets);
        }

        void WatchServices()
        {
            Services ??= new ConcurrentDictionary<string, V1Service>();
            var watch = _serviceClient.CoreV1.ListServiceForAllNamespacesWithHttpMessagesAsync(
                watch: true,
                timeoutSeconds: 60000,
                cancellationToken: SafeTask.cancellationTokenSource.Token
            );
            CreateWatch(watch, Services, WatchServices);
        }

        void WatchIngresses()
        {
            Ingresses ??= new ConcurrentDictionary<string, V1Ingress>();
            var watch = _ingressClient.NetworkingV1.ListIngressForAllNamespacesWithHttpMessagesAsync(
                watch: true,
                timeoutSeconds: 60000,
                cancellationToken: SafeTask.cancellationTokenSource.Token
            );
            CreateWatch(watch, Ingresses, WatchIngresses);
        }

        void WatchPersistentVolumes()
        {
            PersistentVolumes ??= new ConcurrentDictionary<string, V1PersistentVolume>();
            var watch = _persistentVolumeClient.CoreV1.ListPersistentVolumeWithHttpMessagesAsync(
                watch: true,
                timeoutSeconds: 60000,
                cancellationToken: SafeTask.cancellationTokenSource.Token
            );
            CreateWatch(watch, PersistentVolumes, WatchPersistentVolumes);
        }

        void WatchPersistentVolumeClaims()
        {
            PersistentVolumeClaims ??= new ConcurrentDictionary<string, V1PersistentVolumeClaim>();
            var watch = _persistentVolumeClaimClient.CoreV1.ListPersistentVolumeClaimForAllNamespacesWithHttpMessagesAsync(
                watch: true,
                timeoutSeconds: 60000,
                cancellationToken: SafeTask.cancellationTokenSource.Token
            );
            CreateWatch(watch, PersistentVolumeClaims, WatchPersistentVolumeClaims);
        }





    }


}