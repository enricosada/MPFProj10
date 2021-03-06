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

namespace Microsoft.VisualStudio.Project.Automation
{
	using System;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using System.Runtime.InteropServices;
	using EnvDTE;
	using VSLangProj;
	using VSLangProj80;

	/// <summary>
	/// Represents an automation friendly version of a language-specific project.
	/// </summary>
	[SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "OAVS")]
	[ComVisible(true), CLSCompliant(false)]
	public class OAVSProject : VSProject, VSProject2
	{
		private readonly ProjectNode project;
		private OAVSProjectEvents events;

		public OAVSProject(ProjectNode project)
		{
			this.project = project;
		}

		#region VSProject Members

		public virtual ProjectItem AddWebReference(string bstrUrl)
		{
			throw new NotSupportedException();
		}

		public virtual BuildManager BuildManager
		{
			get
			{
				return new OABuildManager(this.project);
			}
		}

		public virtual void CopyProject(string bstrDestFolder, string bstrDestUNCPath, prjCopyProjectOption copyProjectOption, string bstrUsername, string bstrPassword)
		{
			throw new NotSupportedException();
		}

		public virtual ProjectItem CreateWebReferencesFolder()
		{
			throw new NotSupportedException();
		}

		public virtual DTE DTE
		{
			get
			{
				return (EnvDTE.DTE)this.project.Site.GetService(typeof(EnvDTE.DTE));
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public virtual VSProjectEvents Events
		{
			get
			{
				return Events2;
			}
		}

		public virtual void Exec(prjExecCommand command, int bSuppressUI, object varIn, out object pVarOut)
		{
			throw new NotSupportedException();
		}

		public virtual void GenerateKeyPairFiles(string strPublicPrivateFile, string strPublicOnlyFile)
		{
			throw new NotSupportedException();
		}

		public virtual string GetUniqueFilename(object pDispatch, string bstrRoot, string bstrDesiredExt)
		{
			throw new NotSupportedException();
		}

		public virtual Imports Imports
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public virtual EnvDTE.Project Project
		{
			get
			{
				return this.project.GetAutomationObject() as EnvDTE.Project;
			}
		}

		public virtual References References
		{
			get
			{
				ReferenceContainerNode references = project.GetReferenceContainer() as ReferenceContainerNode;
				if(null == references)
				{
					return null;
				}
				return references.Object as References;
			}
		}

		public virtual void Refresh()
		{
			throw new NotSupportedException();
		}

		public virtual string TemplatePath
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public virtual ProjectItem WebReferencesFolder
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		public virtual bool WorkOffline
		{
			get
			{
				throw new NotSupportedException();
			}

			set
			{
				throw new NotSupportedException();
			}
		}

		#endregion

		#region VSProject2 Members

		/// <summary>
		/// Gets a VSProjectEvents2 object that allows you to respond to events of the Imports,
		/// References, BuildManager, and VSLangProjWebReferencesEvents objects.
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public virtual VSProjectEvents2 Events2
		{
			get
			{
				if (events == null)
					events = new OAVSProjectEvents(this);

				return events;
			}
		}

		/// <summary>
		/// Gets a PublishManager object to allow click once publishing.
		/// </summary>
		public virtual object PublishManager
		{
			get
			{
				throw new NotSupportedException();
			}
		}

		#endregion
	}
}
