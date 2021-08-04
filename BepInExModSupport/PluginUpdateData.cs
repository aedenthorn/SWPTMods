using BepInEx;
using System;
using UnityEngine;

namespace BepInExModSupport
{
    public class PluginUpdateData
    {
        public int id;
        public Version remoteVersion;
        public PluginInfo pluginInfo;
        public bool checkable = false;
        public Transform updateSign;
    }
}