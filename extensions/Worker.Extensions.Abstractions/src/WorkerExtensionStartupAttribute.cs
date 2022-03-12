// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace Microsoft.Azure.Functions.Worker.Extensions.Abstractions
{

    [AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
    public class WorkerExtensionStartupAttribute : Attribute
    {
        /// <summary>
        /// Constructs a new instance of <see cref="WorkerExtensionStartupAttribute"/>.
        /// </summary>
        /// <param name="startupType">The type of the startup class implementation.
        /// <param name="name">The friendly human readable name for the startup action. If null, the name will be
        /// defaulted based on naming convention.</param>
        public WorkerExtensionStartupAttribute(Type startupType, string name = null)
        {
            if (startupType == null)
            {
                throw new ArgumentNullException(nameof(startupType));
            }

            // to do: Check this is applied on IWorkerStartup interface implementation.

            if (string.IsNullOrEmpty(name))
            {
                // for a startup class named 'CustomConfigWorkerExtensionStartup' or 'CustomConfigStartup',
                // default to a name 'CustomConfig'
                name = startupType.Name;
                int idx = name.IndexOf("WorkerExtensionStartup");
                if (idx < 0)
                {
                    idx = name.IndexOf("Startup");
                }
                if (idx > 0)
                {
                    name = name.Substring(0, idx);
                }
            }
            WorkerStartupType = startupType;
            Name = name;
        }

        /// <summary>
        /// Gets the type of the startup class implementation.
        /// </summary>
        public Type WorkerStartupType { get; }

        /// <summary>
        /// Gets the friendly human readable name for the startup action.
        /// </summary>
        public string Name { get; }
    }
}
