/********************************************************************************************

Copyright (c) Microsoft Corporation 
All rights reserved. 

Microsoft Public License: 

This license governs use of the accompanying software. If you use the software, you 
accept this license. If you do not accept the license, do not use the software. 

1. Definitions 
The terms "reproduce," "reproduction," "derivative works," and "distribution" have the 
same meaning here as under U.S. copyright law. 
A "contribution" is the original software, or any additions or changes to the software. 
A "contributor" is any person that distributes its contribution under this license. 
"Licensed patents" are a contributor's patent claims that read directly on its contribution. 

2. Grant of Rights 
(A) Copyright Grant- Subject to the terms of this license, including the license conditions 
and limitations in section 3, each contributor grants you a non-exclusive, worldwide, 
royalty-free copyright license to reproduce its contribution, prepare derivative works of 
its contribution, and distribute its contribution or any derivative works that you create. 
(B) Patent Grant- Subject to the terms of this license, including the license conditions 
and limitations in section 3, each contributor grants you a non-exclusive, worldwide, 
royalty-free license under its licensed patents to make, have made, use, sell, offer for 
sale, import, and/or otherwise dispose of its contribution in the software or derivative 
works of the contribution in the software. 

3. Conditions and Limitations 
(A) No Trademark License- This license does not grant you rights to use any contributors' 
name, logo, or trademarks. 
(B) If you bring a patent claim against any contributor over patents that you claim are 
infringed by the software, your patent license from such contributor to the software ends 
automatically. 
(C) If you distribute any portion of the software, you must retain all copyright, patent, 
trademark, and attribution notices that are present in the software. 
(D) If you distribute any portion of the software in source code form, you may do so only 
under this license by including a complete copy of this license with your distribution. 
If you distribute any portion of the software in compiled or object code form, you may only 
do so under a license that complies with this license. 
(E) The software is licensed "as-is." You bear the risk of using it. The contributors give 
no express warranties, guarantees or conditions. You may have additional consumer rights 
under your local laws which this license cannot change. To the extent permitted under your 
local laws, the contributors exclude the implied warranties of merchantability, fitness for 
a particular purpose and non-infringement.

********************************************************************************************/

namespace Microsoft.VisualStudio.Project
{
	using System;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.Runtime.InteropServices;
	using Microsoft.VisualStudio.Shell;
	using Microsoft.VisualStudio.Shell.Interop;

	[CLSCompliant(false), ComVisible(true)]
	public class ProjectReferenceNode : ReferenceNode
	{
		#region fieds
		/// <summary>
		/// The name of the assembly this refernce represents
		/// </summary>
		private Guid referencedProjectGuid;

		private string referencedProjectName = String.Empty;

		private string referencedProjectRelativePath = String.Empty;

		private string referencedProjectFullPath = String.Empty;

		private BuildDependency buildDependency;

		/// <summary>
		/// This is a reference to the automation object for the referenced project.
		/// </summary>
		private EnvDTE.Project referencedProject;

		/// <summary>
		/// This state is controlled by the solution events.
		/// The state is set to false by OnBeforeUnloadProject.
		/// The state is set to true by OnBeforeCloseProject event.
		/// </summary>
		private bool canRemoveReference = true;

		/// <summary>
		/// Possibility for solution listener to update the state on the dangling reference.
		/// It will be set in OnBeforeUnloadProject then the nopde is invalidated then it is reset to false.
		/// </summary>
		private bool isNodeValid;

		#endregion

		#region properties

		public override string Url
		{
			get
			{
				return this.ReferencedProjectFullPath;
			}
		}

		public override bool CanCacheCanonicalName
		{
			get
			{
				return !string.IsNullOrEmpty(ReferencedProjectFullPath);
			}
		}

		public override string Caption
		{
			get
			{
				return this.referencedProjectName;
			}
		}

		public Guid ReferencedProjectGuid
		{
			get
			{
				return this.referencedProjectGuid;
			}
		}

		/// <summary>
		/// Possiblity to shortcut and set the dangling project reference icon.
		/// It is ussually manipulated by solution listsneres who handle reference updates.
		/// </summary>
		public bool IsNodeValid
		{
			get
			{
				return this.isNodeValid;
			}
			set
			{
				this.isNodeValid = value;
			}
		}

		/// <summary>
		/// Controls the state whether this reference can be removed or not. Think of the project unload scenario where the project reference should not be deleted.
		/// </summary>
		public bool CanRemoveReference
		{
			get
			{
				return this.canRemoveReference;
			}
			set
			{
				this.canRemoveReference = value;
			}
		}

		public string ReferencedProjectName
		{
			get { return this.referencedProjectName; }
		}

		/// <summary>
		/// Gets the automation object for the referenced project.
		/// </summary>
        public EnvDTE.Project ReferencedProjectObject
        {
            get
            {
                // If the referenced project is null then re-read.
                if (this.referencedProject == null)
                {

                    // Search for the project in the collection of the projects in the
                    // current solution.
                    EnvDTE.DTE dte = (EnvDTE.DTE)this.ProjectManager.GetService(typeof(EnvDTE.DTE));
                    if ((null == dte) || (null == dte.Solution))
                    {
                        return null;
                    }
                    foreach (EnvDTE.Project prj in dte.Solution.Projects)
                    {
                        //Skip this project if it is an umodeled project (unloaded)
                        if (string.Equals(EnvDTE.Constants.vsProjectKindUnmodeled, prj.Kind, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        // Get the full path of the current project.
                        EnvDTE.Property pathProperty = null;
                        try
                        {
                            if (prj.Properties == null)
                            {
                                continue;
                            }

                            pathProperty = prj.Properties.Item("FullPath");
                            if (null == pathProperty)
                            {
                                // The full path should alway be availabe, but if this is not the
                                // case then we have to skip it.
                                continue;
                            }
                        }
                        catch (ArgumentException)
                        {
                            continue;
                        }
                        string prjPath = pathProperty.Value.ToString();
                        EnvDTE.Property fileNameProperty = null;
                        // Get the name of the project file.
                        try
                        {
                            fileNameProperty = prj.Properties.Item("FileName");
                            if (null == fileNameProperty)
                            {
                                // Again, this should never be the case, but we handle it anyway.
                                continue;
                            }
                        }
                        catch (ArgumentException)
                        {
                            continue;
                        }
                        prjPath = System.IO.Path.Combine(prjPath, fileNameProperty.Value.ToString());

                        // If the full path of this project is the same as the one of this
                        // reference, then we have found the right project.
                        if (NativeMethods.IsSamePath(prjPath, ReferencedProjectFullPath))
                        {
                            this.referencedProject = prj;
                            break;
                        }
                    }
                }

                return this.referencedProject;
            }
            set
            {
                this.referencedProject = value;
            }
        }

		/// <summary>
		/// Gets the full path to the assembly generated by this project.
		/// </summary>
		public string ReferencedProjectOutputPath
		{
			get
			{
				// Make sure that the referenced project implements the automation object.
				if(null == this.ReferencedProjectObject)
				{
					return null;
				}

				// Get the configuration manager from the project.
				EnvDTE.ConfigurationManager confManager = this.ReferencedProjectObject.ConfigurationManager;
				if(null == confManager)
				{
					return null;
				}

				// Get the active configuration.
				EnvDTE.Configuration config = confManager.ActiveConfiguration;
				if(null == config)
				{
					return null;
				}

				// Get the output path for the current configuration.
				EnvDTE.Property outputPathProperty = config.Properties.Item("OutputPath");
				if(null == outputPathProperty)
				{
					return null;
				}

				string outputPath = outputPathProperty.Value.ToString();

				// Ususally the output path is relative to the project path, but it is possible
				// to set it as an absolute path. If it is not absolute, then evaluate its value
				// based on the project directory.
				if(!System.IO.Path.IsPathRooted(outputPath))
				{
					string projectDir = System.IO.Path.GetDirectoryName(ReferencedProjectFullPath);
					outputPath = System.IO.Path.Combine(projectDir, outputPath);
				}

				// Now get the name of the assembly from the project.
				// Some project system throw if the property does not exist. We expect an ArgumentException.
				EnvDTE.Property assemblyNameProperty = null;
				try
				{
					assemblyNameProperty = this.ReferencedProjectObject.Properties.Item("OutputFileName");
				}
				catch(ArgumentException)
				{
				}

				if(null == assemblyNameProperty)
				{
					return null;
				}
				// build the full path adding the name of the assembly to the output path.
				outputPath = System.IO.Path.Combine(outputPath, assemblyNameProperty.Value.ToString());

				return outputPath;
			}
		}

		private Automation.OAProjectReference projectReference;
		public override object Object
		{
			get
			{
				if(null == projectReference)
				{
					projectReference = new Automation.OAProjectReference(this);
				}
				return projectReference;
			}
		}

		protected string ReferencedProjectFullPath
		{
			get
			{
				return referencedProjectFullPath;
			}

			set
			{
				if (referencedProjectFullPath == value)
					return;

				referencedProjectFullPath = value;
				ProjectManager.ItemIdMap.UpdateCanonicalName(this);
			}
		}
		#endregion

		#region ctors
		/// <summary>
		/// Constructor for the ReferenceNode. It is called when the project is reloaded, when the project element representing the refernce exists. 
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2234:PassSystemUriObjectsInsteadOfStrings")]
		public ProjectReferenceNode(ProjectNode root, ProjectElement element)
			: base(root, element)
		{
			this.referencedProjectRelativePath = this.ItemNode.GetMetadata(ProjectFileConstants.Include);
			Debug.Assert(!String.IsNullOrEmpty(this.referencedProjectRelativePath), "Could not retrive referenced project path form project file");

			string guidString = this.ItemNode.GetMetadata(ProjectFileConstants.Project);

			// Continue even if project setttings cannot be read.
			try
			{
				this.referencedProjectGuid = new Guid(guidString);

				this.buildDependency = new BuildDependency(this.ProjectManager, this.referencedProjectGuid);
				this.ProjectManager.AddBuildDependency(this.buildDependency);
			}
			finally
			{
				Debug.Assert(this.referencedProjectGuid != Guid.Empty, "Could not retrive referenced project guidproject file");

				this.referencedProjectName = this.ItemNode.GetMetadata(ProjectFileConstants.Name);

				Debug.Assert(!String.IsNullOrEmpty(this.referencedProjectName), "Could not retrive referenced project name form project file");
			}

			Uri uri = new Uri(this.ProjectManager.BaseUri.Uri, this.referencedProjectRelativePath);

			if(uri != null)
			{
				this.ReferencedProjectFullPath = Microsoft.VisualStudio.Shell.Url.Unescape(uri.LocalPath, true);
			}
		}

		/// <summary>
		/// constructor for the ProjectReferenceNode
		/// </summary>
		public ProjectReferenceNode(ProjectNode root, string referencedProjectName, string projectPath, string projectReference)
			: base(root)
		{
			Debug.Assert(root != null && !String.IsNullOrEmpty(referencedProjectName) && !String.IsNullOrEmpty(projectReference)
				&& !String.IsNullOrEmpty(projectPath), "Can not add a reference because the input for adding one is invalid.");

            if (projectReference == null)
            {
                throw new ArgumentNullException("projectReference");
            }
			
            this.referencedProjectName = referencedProjectName;

			int indexOfSeparator = projectReference.IndexOf('|');


			string fileName = String.Empty;

			// Unfortunately we cannot use the path part of the projectReference string since it is not resolving correctly relative pathes.
			if(indexOfSeparator != -1)
			{
				string projectGuid = projectReference.Substring(0, indexOfSeparator);
				this.referencedProjectGuid = new Guid(projectGuid);
				if(indexOfSeparator + 1 < projectReference.Length)
				{
					string remaining = projectReference.Substring(indexOfSeparator + 1);
					indexOfSeparator = remaining.IndexOf('|');

					if(indexOfSeparator == -1)
					{
						fileName = remaining;
					}
					else
					{
						fileName = remaining.Substring(0, indexOfSeparator);
					}
				}
			}

			Debug.Assert(!String.IsNullOrEmpty(fileName), "Can not add a project reference because the input for adding one is invalid.");

			// Did we get just a file or a relative path?
			Uri uri = new Uri(projectPath);

			string referenceDir = PackageUtilities.GetPathDistance(this.ProjectManager.BaseUri.Uri, uri);

			Debug.Assert(!String.IsNullOrEmpty(referenceDir), "Can not add a project reference because the input for adding one is invalid.");

			string justTheFileName = Path.GetFileName(fileName);
			this.referencedProjectRelativePath = Path.Combine(referenceDir, justTheFileName);

			this.ReferencedProjectFullPath = Path.Combine(projectPath, justTheFileName);

			this.buildDependency = new BuildDependency(this.ProjectManager, this.referencedProjectGuid);

		}
		#endregion

		#region methods
		protected override NodeProperties CreatePropertiesObject()
		{
			return new ProjectReferencesProperties(this);
		}

		/// <summary>
		/// The node is added to the hierarchy and then updates the build dependency list.
		/// </summary>
		public override void AddReference()
		{
			if(this.ProjectManager == null)
			{
				return;
			}
			base.AddReference();
			this.ProjectManager.AddBuildDependency(this.buildDependency);
			return;
		}

		/// <summary>
		/// Overridden method. The method updates the build dependency list before removing the node from the hierarchy.
		/// </summary>
		public override void Remove(bool removeFromStorage)
		{
			if(this.ProjectManager == null || !this.CanRemoveReference)
			{
				return;
			}
			this.ProjectManager.RemoveBuildDependency(this.buildDependency);
			base.Remove(removeFromStorage);
			return;
		}

		/// <summary>
		/// Links a reference node to the project file.
		/// </summary>
		protected override void BindReferenceData()
		{
			Debug.Assert(!String.IsNullOrEmpty(this.referencedProjectName), "The referencedProjectName field has not been initialized");
			Debug.Assert(this.referencedProjectGuid != Guid.Empty, "The referencedProjectName field has not been initialized");

			this.ItemNode = new ProjectElement(this.ProjectManager, this.referencedProjectRelativePath, ProjectFileConstants.ProjectReference);

			this.ItemNode.SetMetadata(ProjectFileConstants.Name, this.referencedProjectName);
			this.ItemNode.SetMetadata(ProjectFileConstants.Project, this.referencedProjectGuid.ToString("B"));
			this.ItemNode.SetMetadata(ProjectFileConstants.Private, true.ToString());
		}

		/// <summary>
		/// Defines whether this node is valid node for painting the refererence icon.
		/// </summary>
		/// <returns></returns>
		protected override bool CanShowDefaultIcon()
		{
			if(this.referencedProjectGuid == Guid.Empty || this.ProjectManager == null || this.ProjectManager.IsClosed || this.isNodeValid)
			{
				return false;
			}

			IVsHierarchy hierarchy = null;

			hierarchy = VsShellUtilities.GetHierarchy(this.ProjectManager.Site, this.referencedProjectGuid);

			if(hierarchy == null)
			{
				return false;
			}

			//If the Project is unloaded return false
			if(this.ReferencedProjectObject == null)
			{
				return false;
			}

			return (!String.IsNullOrEmpty(this.ReferencedProjectFullPath) && File.Exists(this.ReferencedProjectFullPath));
		}

		/// <summary>
		/// Checks if a project reference can be added to the hierarchy. It calls base to see if the reference is not already there, then checks for circular references.
		/// </summary>
		/// <param name="errorHandler">The error handler delegate to return</param>
		/// <returns></returns>
		protected override bool CanAddReference(out CannotAddReferenceErrorMessage errorHandler)
		{
			// When this method is called this refererence has not yet been added to the hierarchy, only instantiated.
			if(!base.CanAddReference(out errorHandler))
			{
				return false;
			}

			errorHandler = null;
			if(this.IsThisProjectReferenceInCycle())
			{
				errorHandler = new CannotAddReferenceErrorMessage(ShowCircularReferenceErrorMessage);
				return false;
			}

			return true;
		}

		protected virtual bool IsThisProjectReferenceInCycle()
		{
			return IsReferenceInCycle(this.referencedProjectGuid);
		}

		protected virtual void ShowCircularReferenceErrorMessage()
		{
			string message = String.Format(CultureInfo.CurrentCulture, SR.GetString(SR.ProjectContainsCircularReferences, CultureInfo.CurrentUICulture), this.referencedProjectName);
			string title = string.Empty;
			OLEMSGICON icon = OLEMSGICON.OLEMSGICON_CRITICAL;
			OLEMSGBUTTON buttons = OLEMSGBUTTON.OLEMSGBUTTON_OK;
			OLEMSGDEFBUTTON defaultButton = OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST;
			VsShellUtilities.ShowMessageBox(this.ProjectManager.Site, title, message, icon, buttons, defaultButton);
		}

		/// <summary>
		/// Checks whether a reference added to a given project would introduce a circular dependency.
		/// </summary>
		protected virtual bool IsReferenceInCycle(Guid projectGuid)
		{
			IVsHierarchy referencedHierarchy = VsShellUtilities.GetHierarchy(this.ProjectManager.Site, projectGuid);

			var solutionBuildManager = this.ProjectManager.Site.GetService(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager2;
			if (solutionBuildManager == null)
			{
				throw new InvalidOperationException("Cannot find the IVsSolutionBuildManager2 service.");
			}

			int circular;
			Marshal.ThrowExceptionForHR(solutionBuildManager.CalculateProjectDependencies());
			Marshal.ThrowExceptionForHR(solutionBuildManager.QueryProjectDependency(referencedHierarchy, this.ProjectManager.InteropSafeIVsHierarchy, out circular));

			return circular != 0;
		}
		#endregion
	}

}
