using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Xml;

namespace MLEventHandler
{
    public class PluginStepHandler : Plugin
    {
        private XmlNodeList EntityConfigList;

        public PluginStepHandler(string unsecureConfig, string secureConfig)
          : base(typeof(PluginStepHandler))
        {
            this.RetrieveUnsecureConfiguration(unsecureConfig);
        }

        private void RetrieveUnsecureConfiguration(string unsecureConfig)
        {
            XmlDocument xmlDocument = new XmlDocument();
            if (string.IsNullOrEmpty(unsecureConfig))
                return;
            try
            {
                xmlDocument.LoadXml(unsecureConfig);
                this.EntityConfigList = xmlDocument["Settings"].ChildNodes;
                this.RegisterEntityEvents(this.EntityConfigList[0].Attributes["name"].Value);
                Dictionary<string, string> dictionary = new Dictionary<string, string>();
                if (this.EntityConfigList.Count > 1)
                {
                    XmlNodeList xmlNodeList = this.EntityConfigList[1].SelectNodes("lookup");
                    if (xmlNodeList != null)
                    {
                        foreach (XmlNode xmlNode in xmlNodeList)
                        {
                            string key = xmlNode.Attributes["name"].Value;
                            if (!dictionary.ContainsKey(key))
                            {
                                dictionary.Add(key, "dummy");
                                this.RegisteredEvents.Add(new Tuple<int, string, string, Action<Plugin.LocalPluginContext>>(40, "Retrieve", key, new Action<Plugin.LocalPluginContext>(this.UnpackNameOnRetrieve)));
                                this.RegisteredEvents.Add(new Tuple<int, string, string, Action<Plugin.LocalPluginContext>>(40, "RetrieveMultiple", key, new Action<Plugin.LocalPluginContext>(this.UnpackNameOnRetrieveMultiple)));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException("Invalid xml configuration setting - " + ex.ToString());
            }
        }

        private void RegisterEntityEvents(string entityName)
        {
            this.RegisteredEvents.Add(new Tuple<int, string, string, Action<Plugin.LocalPluginContext>>(20, "Create", entityName, new Action<Plugin.LocalPluginContext>(this.PackNameTranslations)));
            this.RegisteredEvents.Add(new Tuple<int, string, string, Action<Plugin.LocalPluginContext>>(20, "Update", entityName, new Action<Plugin.LocalPluginContext>(this.PackNameTranslations)));
            this.RegisteredEvents.Add(new Tuple<int, string, string, Action<Plugin.LocalPluginContext>>(40, "Retrieve", entityName, new Action<Plugin.LocalPluginContext>(this.UnpackNameOnRetrieve)));
            this.RegisteredEvents.Add(new Tuple<int, string, string, Action<Plugin.LocalPluginContext>>(40, "RetrieveMultiple", entityName, new Action<Plugin.LocalPluginContext>(this.UnpackNameOnRetrieveMultiple)));
        }

        protected void PackNameTranslations(Plugin.LocalPluginContext localContext)
        {
            IPluginExecutionContext executionContext = localContext.PluginExecutionContext;
            Entity targetEntity = this.GetTargetEntity(executionContext);
            Entity preImageEntity = this.GetPreImageEntity(executionContext);
            StringBuilder stringBuilder = new StringBuilder();
            if (this.EntityConfigList == null)
                return;
            foreach (XmlNode selectNode1 in this.EntityConfigList[0].SelectNodes("fields"))
            {
                foreach (XmlNode selectNode2 in selectNode1.SelectNodes("field"))
                {
                    stringBuilder.Append(this.GetAttributeValue<string>(selectNode2.InnerText, preImageEntity, targetEntity));
                    stringBuilder.Append('|');
                }
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
                this.SetTargetAttribute(targetEntity, selectNode1.Attributes["name"].Value, stringBuilder.ToString());
                stringBuilder.Clear();
            }
        }

        protected void UnpackNameOnRetrieve(Plugin.LocalPluginContext localContext)
        {
            IPluginExecutionContext executionContext = localContext.PluginExecutionContext;
            ITracingService tracingService = localContext.TracingService;
            if (this.CheckInitiatingUserDisabled(localContext, tracingService))
                return;
            Entity outputParameter = (Entity)executionContext.OutputParameters["BusinessEntity"];
            this.UnpackAttributeValue(localContext, outputParameter);
        }

        protected void UnpackNameOnRetrieveMultiple(Plugin.LocalPluginContext localContext)
        {
            IPluginExecutionContext executionContext = localContext.PluginExecutionContext;
            ITracingService tracingService = localContext.TracingService;
            if (this.CheckInitiatingUserDisabled(localContext, tracingService))
                return;
            EntityCollection outputParameter = (EntityCollection)localContext.PluginExecutionContext.OutputParameters["BusinessEntityCollection"];
            if (outputParameter.Entities.Count == 0)
                return;
            Entity entity1 = new Entity();
            foreach (Entity entity2 in (Collection<Entity>)outputParameter.Entities)
            {
                entity1 = entity2;
                this.UnpackAttributeValueMultiple(localContext, entity2);
            }
        }

        private void UnpackAttributeValue(Plugin.LocalPluginContext localContext, Entity target)
        {
            if (this.EntityConfigList == null)
                return;
            XmlNodeList xmlNodeList1 = this.EntityConfigList[0].SelectNodes("fields");
            string empty1 = string.Empty;
            string empty2 = string.Empty;
            string empty3 = string.Empty;
            foreach (XmlNode lcids in xmlNodeList1)
            {
                string index = lcids.Attributes["name"].Value;
                empty2 = string.Empty;
                if (target.Contains(index))
                {
                    string attributeValue = this.GetAttributeValue<string>(index, (Entity)null, target);
                    string name = this.UnpackName(localContext, attributeValue, lcids);
                    if (target[index].GetType() == typeof(EntityReference))
                        ((EntityReference)target[index]).Name = name;
                    else
                        this.SetTargetAttribute(target, index, name);
                }
            }
            if (this.EntityConfigList.Count > 1)
            {
                XmlNodeList xmlNodeList2 = this.EntityConfigList[1].SelectNodes("lookup");
                XmlNodeList xmlNodeList3 = this.EntityConfigList[0].SelectNodes("fields");
                if (xmlNodeList2 != null)
                {
                    foreach (XmlNode xmlNode1 in xmlNodeList2)
                    {
                        XmlNodeList xmlNodeList4 = xmlNode1.SelectNodes("field");
                        if (xmlNodeList4 != null)
                        {
                            foreach (XmlNode xmlNode2 in xmlNodeList4)
                            {
                                string innerText = xmlNode2.InnerText;
                                if (target.Contains(innerText))
                                {
                                    string str = this.UnpackName(localContext, ((EntityReference)target[innerText]).Name, xmlNodeList3[0]);
                                    ((EntityReference)target[innerText]).Name = str;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void UnpackAttributeValueMultiple(Plugin.LocalPluginContext localContext, Entity target)
        {
            if (this.EntityConfigList == null)
                return;
            XmlNodeList xmlNodeList1 = this.EntityConfigList[0].SelectNodes("fields");
            string empty1 = string.Empty;
            string empty2 = string.Empty;
            foreach (XmlNode lcids in xmlNodeList1)
            {
                string attributeName = lcids.Attributes["name"].Value;
                if (target.Contains(attributeName))
                {
                    string attributeValue = this.GetAttributeValue<string>(attributeName, (Entity)null, target);
                    string str = this.UnpackName(localContext, attributeValue, lcids);
                    if (target[attributeName].GetType() == typeof(EntityReference))
                        ((EntityReference)target[attributeName]).Name = str;
                    else
                        target[attributeName] = (object)str;
                }
            }
            if (this.EntityConfigList.Count > 1)
            {
                XmlNodeList xmlNodeList2 = this.EntityConfigList[1].SelectNodes("lookup");
                XmlNodeList xmlNodeList3 = this.EntityConfigList[0].SelectNodes("fields");
                if (xmlNodeList2 != null)
                {
                    foreach (XmlNode xmlNode1 in xmlNodeList2)
                    {
                        XmlNodeList xmlNodeList4 = xmlNode1.SelectNodes("field");
                        if (xmlNodeList4 != null)
                        {
                            foreach (XmlNode xmlNode2 in xmlNodeList4)
                            {
                                string innerText = xmlNode2.InnerText;
                                if (target.Contains(innerText))
                                {
                                    string str = this.UnpackName(localContext, ((EntityReference)target[innerText]).Name, xmlNodeList3[0]);
                                    ((EntityReference)target[innerText]).Name = str;
                                }
                            }
                        }
                    }
                }
            }
        }

        protected string UnpackName(Plugin.LocalPluginContext localContext, string name, XmlNode lcids = null)
        {
            int userLanguageId = this.GetUserLanguageId(localContext);
            XmlNodeList xmlNodeList = lcids.SelectNodes("field");
            int[] array = new int[xmlNodeList.Count];
            for (int index = 0; index < xmlNodeList.Count; ++index)
                array[index] = int.Parse(xmlNodeList[index].Attributes["lcid"].Value);
            string[] strArray = name.Split('|');
            int index1 = Array.IndexOf<int>(array, userLanguageId);
            if (index1 < 0 || index1 > strArray.Length - 1)
                index1 = 0;
            return strArray[index1];
        }

        private int GetUserLanguageId(Plugin.LocalPluginContext localContext)
        {
            string attributeLogicalName = "uilanguageid";
            string key = "UserLocaleId";
            int num;
            if (localContext.PluginExecutionContext.SharedVariables.ContainsKey(key))
            {
                num = (int)localContext.PluginExecutionContext.SharedVariables[key];
            }
            else
            {
                num = localContext.OrganizationService.Retrieve("usersettings", localContext.PluginExecutionContext.InitiatingUserId, new ColumnSet(new string[1]
                {
          attributeLogicalName
                })).GetAttributeValue<int>(attributeLogicalName);
                localContext.PluginExecutionContext.SharedVariables.Add(key, (object)num);
            }
            return num;
        }

        private T GetAttributeValue<T>(string attributeName, Entity preImage, Entity targetImage)
        {
            if (targetImage.Contains(attributeName))
                return targetImage.GetAttributeValue<T>(attributeName);
            if (preImage != null && preImage.Contains(attributeName))
                return preImage.GetAttributeValue<T>(attributeName);
            return default(T);
        }

        private void SetTargetAttribute(Entity target, string primaryAttributeName, string name)
        {
            if (!target.Contains(primaryAttributeName))
                target.Attributes.Add(primaryAttributeName, (object)name);
            else
                target[primaryAttributeName] = (object)name;
        }

        private Entity GetTargetEntity(IPluginExecutionContext context)
        {
            return (Entity)context.InputParameters["Target"];
        }

        private Entity GetPreImageEntity(IPluginExecutionContext context)
        {
            return context.PreEntityImages == null || !context.PreEntityImages.Contains("PreImage") ? (Entity)null : context.PreEntityImages["PreImage"];
        }

        private bool CheckInitiatingUserDisabled(
          Plugin.LocalPluginContext localContext,
          ITracingService tracer)
        {
            Entity entity = localContext.OrganizationService.Retrieve("systemuser", localContext.PluginExecutionContext.InitiatingUserId, new ColumnSet(new string[1]
            {
        "isdisabled"
            }));
            if (entity == null || !entity.Attributes.Contains("isdisabled"))
                return true;
            if (!(bool)entity.Attributes["isdisabled"])
                return false;
            tracer.Trace("the initiating user is disabled.");
            return true;
        }
    }
}
