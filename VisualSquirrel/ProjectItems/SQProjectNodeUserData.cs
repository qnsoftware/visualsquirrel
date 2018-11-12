/* see LICENSE notice in solution root */

using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Project;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace VisualSquirrel
{
    public abstract class SQProjectNode : ProjectNode
    {
        HashSet<string> _userproperties = new HashSet<string>();
        public void RegisterUserProperty(string propertyname)
        {
            _userproperties.Add(propertyname);
        }        
        public virtual void OnUserPropertyLoad(string propertyname, string value)
        {

        }
        protected override void LoadNonBuildInformation()
        {
            string filepath = this.FileName + ".user";
            if (File.Exists(filepath))
            {
                Microsoft.Build.Evaluation.Project pb = new Microsoft.Build.Evaluation.Project();
                BuildProject.DisableMarkDirty = true;
                var projuser = pb.ProjectCollection.LoadProject(filepath);
                foreach (string key in _userproperties)
                {
                    try
                    {
                        foreach (var group in projuser.Xml.PropertyGroups)
                        {
                            bool gotit = false;
                            foreach (var p in group.Properties)
                            {
                                if (p.Name == key)
                                {                                    
                                    BuildProject.SetGlobalProperty(key, p.Value);
                                    OnUserPropertyLoad(key, p.Value);
                                    gotit = true;
                                    break;
                                }
                            }
                            if (gotit)
                                break;
                        }

                    }
                    catch (Exception)
                    {
                        //
                    }
                }
                pb.ProjectCollection.UnloadProject(projuser);
                BuildProject.DisableMarkDirty = false;
                BuildProject.Save();
            }            
            base.LoadNonBuildInformation();
        }
        public override int Save(string fileToBeSaved, int remember, uint formatIndex)
        {
            Microsoft.Build.Evaluation.Project pb = new Microsoft.Build.Evaluation.Project();
            var g = pb.Xml.AddPropertyGroup();
            //List<ProjectProperty> toreturn = new List<ProjectProperty>();
            foreach (string key in _userproperties)
            {
                var prop = BuildProject.GetProperty(key);
                if (prop != null)
                {
                    try
                    {
                        //BuildProject.RemoveProperty(prop);
                        var element = g.AddProperty(key, prop.EvaluatedValue);
                        //toreturn.Add(prop);
                    }
                    catch (Exception)
                    {
                        //
                    }
                }
            }
            pb.Xml.Save(this.FileName + ".user");
            pb.ProjectCollection.UnloadProject(pb);

            if (base.Save(fileToBeSaved, remember, formatIndex) == VSConstants.S_OK)
            {
                //nothing to see here, move along.
            }
            /*foreach (var prop in toreturn)
            {                                              
                BuildProject.SetProperty(prop.Name, prop.UnevaluatedValue);
            }*/
            //base.SetProjectFileDirty(false);
            return VSConstants.S_OK;
        }       
    }
}
