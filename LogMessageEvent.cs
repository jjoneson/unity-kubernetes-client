using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Events;

namespace UnityKubernetesClient
{
    [System.Serializable]
    public class LogMessageEvent : UnityEvent<String>
    {
    }
}
