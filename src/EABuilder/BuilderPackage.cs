using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using Microsoft.Build.Evaluation;
using Project = EnvDTE.Project;
using ProjectItem = EnvDTE.ProjectItem;

namespace EABuilder
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [Guid(BuilderPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class BuilderPackage : Package
    {
        private DTE _dte;

        /// <summary>
        /// BuilderPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "edcab9d3-e160-4035-abac-fa5feae46bc6";

        /// <summary>
        /// Initializes a new instance of the <see cref="BuilderPackage"/> class.
        /// </summary>
        public BuilderPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            _dte = (DTE) GetService(typeof (SDTE));
            var buildEvents = _dte.Events.BuildEvents;
            buildEvents.OnBuildProjConfigDone += BuildEvents_OnBuildProjConfigDone;

            var outputLog = GetOutputPane(VSConstants.OutputWindowPaneGuid.BuildOutputPane_guid, "Build");
            var statusBar = (IVsStatusbar) GetService(typeof (SVsStatusbar));
            Log.Instantiate(outputLog, statusBar);
        }

        private void BuildEvents_OnBuildProjConfigDone(string project, string projectConfig, string platform, string solutionConfig, bool success)
        {
            if (!success)
                return;

            var proj = FindProject(project);
            var target = GetPropertyValueFrom(proj.FullName, "TargetPath");
            string[] errors;
            Compiler.Execute(target, out errors);

            if (errors.Length > 0)
                _dte.ExecuteCommand("Build.Cancel");
        }

        private Project FindProject(string projectUniqueName)
        {
            var solution = _dte.Solution;
            foreach (Project project in solution.Projects)
            {
                if (projectUniqueName == null
                    || project.UniqueName == projectUniqueName)
                    return project;

                if (project.ProjectItems != null)
                {
                    var subProject = FindProject(projectUniqueName, project.ProjectItems);
                    if (subProject != null)
                        return subProject;
                }
            }

            return null;
        }

        private Project FindProject(string projectUniqueName, ProjectItems parent)
        {
            foreach (ProjectItem projectItem in parent)
            {
                if (projectItem.SubProject == null)
                    continue;

                if (projectUniqueName == null
                    || projectItem.SubProject.UniqueName == projectUniqueName)
                    return projectItem.SubProject;

                var subProject = FindProject(projectUniqueName, projectItem.SubProject.ProjectItems);
                if (subProject != null)
                    return subProject;
            }

            return null;
        }    

        public static string GetPropertyValueFrom(string projectFile, string propertyName)
        {
            using (var projectCollection = new ProjectCollection())
            {
                var project = new Microsoft.Build.Evaluation.Project(
                    projectFile, null, null, projectCollection, ProjectLoadSettings.Default);

                return project.Properties
                    .Where(x => x.Name == propertyName)
                    .Select(x => x.EvaluatedValue)
                    .SingleOrDefault();
            }
        }
    }

    #endregion
}
