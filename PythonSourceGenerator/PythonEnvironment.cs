﻿using Python.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace PythonSourceGenerator
{
    // This is a copy of PythonEnvironments/PythonEnvironment.cs
    public class PythonEnvironment(string pythonLocation, string version = "3.10.0")
    {
        private readonly string versionPath = MapVersion(version);
        private PythonEnvironmentInternal env;
        private string[] extraPaths = [];
        private readonly IFormatter formatter = new PythonFormatter();

        private static string MapVersion(string version, string sep = "")
        {
            // split on . then take the first two segments and join them without spaces
            var versionParts = version.Split('.');
            return string.Join("", versionParts.Take(2));
        }

        public PythonEnvironment(string version = "3.10") : this(TryLocatePython(version), version)
        {
        }

        public PythonEnvironment WithVirtualEnvironment(string path)
        {
            extraPaths = [.. extraPaths, path];
            return this;
        }

        public IPythonEnvironment Build(string home)
        {
            if (PythonEngine.IsInitialized)
            {
                // Raise exception?
                return env;
            }
            env = new PythonEnvironmentInternal(pythonLocation, versionPath, home, this, this.extraPaths, formatter);

            return env;
        }

        private static string TryLocatePython(string version)
        {
            var versionPath = MapVersion(version);
            var windowsStorePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Python", "Python" + versionPath);
            var officialInstallerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Python", MapVersion(version, "."));
            // TODO: Locate from PATH
            // TODO: Add standard paths for Linux and MacOS
            if (Directory.Exists(windowsStorePath))
            {
                return windowsStorePath;
            }
            else if (Directory.Exists(officialInstallerPath))
            {
                return officialInstallerPath;
            }
            else
            {
                // TODO : Use nuget package path?
                throw new Exception("Python not found");
            }
        }

        public override bool Equals(object obj)
        {
            return obj is PythonEnvironment environment &&
                   versionPath == environment.versionPath;
        }

        public override int GetHashCode()
        {
            int hashCode = 955711454;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(versionPath);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(pythonLocation);
            return hashCode;
        }

        internal class PythonFormatter : IFormatter
        {
            public SerializationBinder Binder { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public StreamingContext Context { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public ISurrogateSelector SurrogateSelector { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            private object cachedGraph;

            public object Deserialize(Stream serializationStream) => cachedGraph;

            public void Serialize(Stream serializationStream, object graph) => cachedGraph = graph;
        }

        internal class PythonEnvironmentInternal : IPythonEnvironment
        {
            private readonly PythonEnvironment pythonEnvironment;

            public PythonEnvironmentInternal(string pythonLocation, string versionPath, string home, PythonEnvironment pythonEnvironment, string[] extraPath, IFormatter formatter)
            {
                // Don't use BinaryFormatter by default as it's deprecated in .NET 8
                // See https://github.com/pythonnet/pythonnet/issues/2282
                RuntimeData.FormatterFactory = () => formatter;
                var dllPath = Path.Combine(pythonLocation, string.Format("python{0}.dll", versionPath));
                if (!File.Exists(dllPath))
                {
                    throw new FileNotFoundException("Python DLL not found", dllPath);
                }
                Runtime.PythonDLL = dllPath; string sep = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ";" : ":";
                if (!string.IsNullOrEmpty(home))
                {
                    PythonEngine.PythonHome = home;
                    PythonEngine.PythonPath = Path.Combine(pythonLocation, "Lib") + sep + home;
                }
                else
                {
                    PythonEngine.PythonPath = Path.Combine(pythonLocation, "Lib");
                }

                if (extraPath.Length > 0)
                {
                    PythonEngine.PythonPath = PythonEngine.PythonPath + sep + string.Join(sep, extraPath);
                }
                try
                {
                    PythonEngine.Initialize();
                }
                catch (NullReferenceException)
                {

                }
                this.pythonEnvironment = pythonEnvironment;
            }

            public void Dispose()
            {
                try
                {
                    //PythonEngine.Shutdown();
                }
                catch (NotImplementedException) // Thrown from NoopFormatter
                {
                    // Ignore
                }
                pythonEnvironment.Destroy();
            }
        }

        private void Destroy()
        {
            env = null;
        }
    }

    public interface IPythonEnvironment : IDisposable
    {
    }
}