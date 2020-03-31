using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Text;

namespace MLConfiguration
{
    public class PluginConfig : Plugin
    {
        public PluginConfig()
          : base(typeof(PluginConfig))
        {
            this.RegisterEntityEvents();
        }

        private void RegisterEntityEvents()
        {
            this.RegisteredEvents.Add(new Tuple<int, string, string, Action<Plugin.LocalPluginContext>>(40, "Create", "chi_multilinguallookupconfiguration", new Action<Plugin.LocalPluginContext>(this.OnCreateEntity)));
            this.RegisteredEvents.Add(new Tuple<int, string, string, Action<Plugin.LocalPluginContext>>(40, "Update", "chi_multilinguallookupconfiguration", new Action<Plugin.LocalPluginContext>(this.OnUpdateEntity)));
            this.RegisteredEvents.Add(new Tuple<int, string, string, Action<Plugin.LocalPluginContext>>(40, "Delete", "chi_multilinguallookupconfiguration", new Action<Plugin.LocalPluginContext>(this.OnDeleteEntity)));
        }

        private void OnCreateEntity(Plugin.LocalPluginContext localContext)
        {
            string str1 = string.Empty;
            string primaryAttributes = string.Empty;
            string relatedAttributes = string.Empty;
            IOrganizationService organizationService = localContext.OrganizationService;
            Entity targetEntity = ConfigHelper.GetTargetEntity(localContext.PluginExecutionContext);
            if (targetEntity.Attributes.Contains("chi_name"))
                str1 = targetEntity.GetAttributeValue<string>("chi_name");
            if (string.IsNullOrEmpty(str1))
                throw new InvalidPluginExecutionException("Primary Entity Can Not be NULL.");
            ConfigHelper.IfEntityExist(organizationService, str1);
            if (targetEntity.Attributes.Contains("chi_primaryfields"))
                primaryAttributes = targetEntity.GetAttributeValue<string>("chi_primaryfields");
            if (targetEntity.Attributes.Contains("chi_relatedentitiesandfields"))
                relatedAttributes = targetEntity.GetAttributeValue<string>("chi_relatedentitiesandfields");
            StringBuilder stringBuilder1 = new StringBuilder();
            stringBuilder1.Append((object)ConfigHelper.RegisterStep(organizationService, "Create", PluginStep.PluginStepStage.PreOperation, str1, primaryAttributes, relatedAttributes));
            stringBuilder1.Append(',');
            stringBuilder1.Append((object)ConfigHelper.RegisterStep(organizationService, "Update", PluginStep.PluginStepStage.PreOperation, str1, primaryAttributes, relatedAttributes));
            stringBuilder1.Append(',');
            stringBuilder1.Append((object)ConfigHelper.RegisterStep(organizationService, "Retrieve", PluginStep.PluginStepStage.PostOperation, str1, primaryAttributes, relatedAttributes));
            stringBuilder1.Append(',');
            stringBuilder1.Append((object)ConfigHelper.RegisterStep(organizationService, "RetrieveMultiple", PluginStep.PluginStepStage.PostOperation, str1, primaryAttributes, relatedAttributes));
            ConfigHelper.SetAttribute(targetEntity, "chi_primarystepguids", stringBuilder1.ToString());
            if (!string.IsNullOrEmpty(relatedAttributes))
            {
                StringBuilder stringBuilder2 = new StringBuilder();
                string str2 = relatedAttributes;
                char[] chArray1 = new char[1] { ',' };
                foreach (string str3 in str2.Split(chArray1))
                {
                    char[] chArray2 = new char[1] { '|' };
                    string[] strArray = str3.Split(chArray2);
                    if (str1 != strArray[0])
                    {
                        stringBuilder2.Append((object)ConfigHelper.RegisterStep(organizationService, "Retrieve", PluginStep.PluginStepStage.PostOperation, strArray[0], primaryAttributes, relatedAttributes));
                        stringBuilder2.Append(',');
                        stringBuilder2.Append((object)ConfigHelper.RegisterStep(organizationService, "RetrieveMultiple", PluginStep.PluginStepStage.PostOperation, strArray[0], primaryAttributes, relatedAttributes));
                        stringBuilder2.Append(',');
                    }
                }
                if (!string.IsNullOrEmpty(stringBuilder2.ToString()))
                {
                    stringBuilder2.Remove(stringBuilder2.Length - 1, 1);
                    ConfigHelper.SetAttribute(targetEntity, "chi_relatedstepguids", stringBuilder2.ToString());
                }
            }
            organizationService.Update(targetEntity);
        }

        private void OnUpdateEntity(Plugin.LocalPluginContext localContext)
        {
            if (localContext.PluginExecutionContext.Depth > 1)
                return;
            Entity preEntityImage = localContext.PluginExecutionContext.PreEntityImages["PreImage"];
            Entity postEntityImage = localContext.PluginExecutionContext.PostEntityImages["PostImage"];
            string attributeValue1 = postEntityImage.GetAttributeValue<string>("chi_name");
            string attributeValue2 = postEntityImage.GetAttributeValue<string>("chi_primaryfields");
            string empty1 = string.Empty;
            string attributeValue3 = postEntityImage.GetAttributeValue<string>("chi_relatedentitiesandfields");
            string[] strArray1 = postEntityImage.GetAttributeValue<string>("chi_primarystepguids").Split(',');
            string str1 = "OnUpdate" + attributeValue1;
            foreach (string g in strArray1)
            {
                Guid id = new Guid(g);
                if (id != Guid.Empty)
                {
                    Entity entity1 = localContext.OrganizationService.Retrieve("sdkmessageprocessingstep", id, new ColumnSet(true));
                    if (entity1 != null)
                    {
                        string empty2 = string.Empty;
                        entity1.Attributes["configuration"] = (object)ConfigHelper.ProcessAttributes(attributeValue1, attributeValue2, ref empty2, attributeValue3);
                        string attributeValue4 = entity1.GetAttributeValue<string>("name");
                        localContext.OrganizationService.Update(entity1);
                        if (attributeValue4.StartsWith(str1, StringComparison.InvariantCultureIgnoreCase) && !string.IsNullOrEmpty(empty2))
                        {
                            Entity entity2 = ConfigHelper.RetrieveEntity(localContext.OrganizationService, "sdkmessageprocessingstepimage", new string[1]
                            {
                "sdkmessageprocessingstepid"
                            }, (object[])new string[1] { g }, new ColumnSet(new string[1]
                            {
                "attributes"
                            }), ConditionOperator.Equal);
                            if (entity2 != null && entity2.Attributes.Contains("attributes"))
                            {
                                entity2["attributes"] = (object)empty2;
                                UpdateRequest updateRequest = new UpdateRequest()
                                {
                                    Target = entity2
                                };
                                localContext.OrganizationService.Execute((OrganizationRequest)updateRequest);
                            }
                        }
                    }
                }
            }
            string attributeValue5 = preEntityImage.GetAttributeValue<string>("chi_relatedentitiesandfields");
            string attributeValue6 = postEntityImage.GetAttributeValue<string>("chi_relatedentitiesandfields");
            if (string.IsNullOrEmpty(attributeValue5))
            {
                if (string.IsNullOrEmpty(attributeValue6))
                    return;
                StringBuilder stringBuilder = new StringBuilder();
                string str2 = attributeValue6;
                char[] chArray1 = new char[1] { ',' };
                foreach (string str3 in str2.Split(chArray1))
                {
                    char[] chArray2 = new char[1] { '|' };
                    string[] strArray2 = str3.Split(chArray2);
                    if (attributeValue1 != strArray2[0])
                    {
                        stringBuilder.Append((object)ConfigHelper.RegisterStep(localContext.OrganizationService, "Retrieve", PluginStep.PluginStepStage.PostOperation, strArray2[0], attributeValue2, attributeValue3));
                        stringBuilder.Append(',');
                        stringBuilder.Append((object)ConfigHelper.RegisterStep(localContext.OrganizationService, "RetrieveMultiple", PluginStep.PluginStepStage.PostOperation, strArray2[0], attributeValue2, attributeValue3));
                        stringBuilder.Append(',');
                    }
                }
                if (string.IsNullOrEmpty(stringBuilder.ToString()))
                    return;
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
                ConfigHelper.SetAttribute(postEntityImage, "chi_relatedstepguids", stringBuilder.ToString());
                localContext.OrganizationService.Update(postEntityImage);
            }
            else
            {
                if (attributeValue5 == attributeValue6)
                    return;
                string[] strArray2 = preEntityImage.GetAttributeValue<string>("chi_relatedstepguids").Split(',');
                Guid empty2 = Guid.Empty;
                foreach (string input in strArray2)
                {
                    Guid result = Guid.Empty;
                    if (!string.IsNullOrEmpty(input) && Guid.TryParse(input, out result))
                        ConfigHelper.UnRegisterStep(localContext.OrganizationService, result);
                }
                if (string.IsNullOrEmpty(attributeValue6))
                {
                    ConfigHelper.SetAttribute(postEntityImage, "chi_relatedstepguids", (string)null);
                    localContext.OrganizationService.Update(postEntityImage);
                }
                else
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    string str2 = attributeValue6;
                    char[] chArray1 = new char[1] { ',' };
                    foreach (string str3 in str2.Split(chArray1))
                    {
                        char[] chArray2 = new char[1] { '|' };
                        string[] strArray3 = str3.Split(chArray2);
                        if (attributeValue1 != strArray3[0])
                        {
                            stringBuilder.Append((object)ConfigHelper.RegisterStep(localContext.OrganizationService, "Retrieve", PluginStep.PluginStepStage.PostOperation, strArray3[0], attributeValue2, attributeValue3));
                            stringBuilder.Append(',');
                            stringBuilder.Append((object)ConfigHelper.RegisterStep(localContext.OrganizationService, "RetrieveMultiple", PluginStep.PluginStepStage.PostOperation, strArray3[0], attributeValue2, attributeValue3));
                            stringBuilder.Append(',');
                        }
                    }
                    if (!string.IsNullOrEmpty(stringBuilder.ToString()))
                    {
                        stringBuilder.Remove(stringBuilder.Length - 1, 1);
                        ConfigHelper.SetAttribute(postEntityImage, "chi_relatedstepguids", stringBuilder.ToString());
                        localContext.OrganizationService.Update(postEntityImage);
                    }
                }
            }
        }

        private void OnDeleteEntity(Plugin.LocalPluginContext localContext)
        {
            Entity preEntityImage = localContext.PluginExecutionContext.PreEntityImages["PreImage"];
            Guid empty = Guid.Empty;
            string str1 = preEntityImage.Attributes["chi_primarystepguids"].ToString();
            char[] chArray1 = new char[1] { ',' };
            Guid result;
            foreach (string input in str1.Split(chArray1))
            {
                result = Guid.Empty;
                if (!string.IsNullOrEmpty(input) && Guid.TryParse(input, out result))
                    ConfigHelper.UnRegisterStep(localContext.OrganizationService, result);
            }
            if (!preEntityImage.Attributes.Contains("chi_relatedstepguids"))
                return;
            string str2 = preEntityImage.Attributes["chi_relatedstepguids"].ToString();
            char[] chArray2 = new char[1] { ',' };
            foreach (string input in str2.Split(chArray2))
            {
                result = Guid.Empty;
                if (!string.IsNullOrEmpty(input) && Guid.TryParse(input, out result))
                    ConfigHelper.UnRegisterStep(localContext.OrganizationService, result);
            }
        }
    }
}