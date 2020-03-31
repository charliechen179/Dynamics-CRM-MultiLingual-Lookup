using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace MLConfiguration
{
    public static class ConfigHelper
    {
        public static string ProcessAttributes(
          string entityName,
          string attributes,
          ref string preImageAttributes,
          string lookups = "")
        {
            StringBuilder stringBuilder = new StringBuilder("<Settings><entity name=\"");
            stringBuilder.Append(entityName + "\">");
            string str1 = attributes;
            char[] chArray1 = new char[1] { ',' };
            foreach (string input in str1.Split(chArray1))
            {
                if (Regex.IsMatch(input, "(.*?)\\[(.*?)\\]"))
                {
                    Match match = Regex.Match(input, "(.*?)\\[(.*?)\\]");
                    stringBuilder.Append("<fields name=\"");
                    stringBuilder.Append(match.Groups[1].Value);
                    stringBuilder.Append("\">");
                    string str2 = match.Groups[2].Value;
                    char[] chArray2 = new char[1] { '|' };
                    foreach (string str3 in str2.Split(chArray2))
                    {
                        char[] chArray3 = new char[1] { ';' };
                        string[] strArray = str3.Split(chArray3);
                        stringBuilder.Append("<field lcid=\"");
                        stringBuilder.Append(strArray[1]);
                        stringBuilder.Append("\">");
                        stringBuilder.Append(strArray[0]);
                        stringBuilder.Append("</field>");
                        preImageAttributes = preImageAttributes + strArray[0] + ",";
                    }
                    stringBuilder.Append("</fields>");
                }
            }
            stringBuilder.Append("</entity>");
            if (preImageAttributes.Length > 0)
                preImageAttributes = preImageAttributes.Substring(0, preImageAttributes.Length - 1);
            if (!string.IsNullOrEmpty(lookups))
            {
                stringBuilder.Append("<lookups>");
                string str2 = lookups;
                char[] chArray2 = new char[1] { ',' };
                foreach (string str3 in str2.Split(chArray2))
                {
                    char[] chArray3 = new char[1] { '|' };
                    string[] strArray = str3.Split(chArray3);
                    stringBuilder.Append("<lookup name=\"" + strArray[0] + "\">");
                    for (int index = 1; index < strArray.Length; ++index)
                        stringBuilder.Append("<field>" + strArray[index] + "</field>");
                    stringBuilder.Append("</lookup>");
                }
                stringBuilder.Append("</lookups>");
            }
            stringBuilder.Append("</Settings>");
            return stringBuilder.ToString();
        }

        public static bool IfEntityExist(IOrganizationService service, string entityName)
        {
            bool flag = false;
            foreach (EntityMetadata entityMetadata in ((RetrieveAllEntitiesResponse)service.Execute((OrganizationRequest)new RetrieveAllEntitiesRequest()
            {
                EntityFilters = EntityFilters.Entity,
                RetrieveAsIfPublished = true
            })).EntityMetadata)
            {
                if (entityMetadata.LogicalName == entityName)
                {
                    flag = true;
                    break;
                }
            }
            if (!flag)
                throw new InvalidPluginExecutionException("Entity '" + entityName + "' doesn't exist.");
            return flag;
        }

        public static void SetAttribute(Entity entity, string field, string value)
        {
            if (!entity.Contains(field))
                entity.Attributes.Add(field, (object)value);
            else
                entity[field] = (object)value;
        }

        public static Entity GetTargetEntity(IPluginExecutionContext context)
        {
            Entity entity = (Entity)null;
            if (context.InputParameters.Contains("Target") && context.InputParameters["Target"] is Entity)
                entity = (Entity)context.InputParameters["Target"] ?? context.PreEntityImages["PreImage"];
            return entity;
        }

        public static Entity DoesStepAlreadyRegistered(
          IOrganizationService service,
          string stepName,
          string eventHandlerName)
        {
            string query = "<fetch version=\"1.0\" output-format=\"xml-platform\" mapping=\"logical\" distinct=\"false\"><entity name=\"sdkmessageprocessingstep\"><attribute name=\"name\" /><attribute name=\"configuration\" /><filter type=\"and\"><condition attribute=\"name\" operator=\"eq\" value=\"" + stepName + "\" /><condition attribute=\"eventhandlername\" operator=\"eq\" value=\"" + eventHandlerName + "\" /></filter></entity></fetch > ";
            EntityCollection entityCollection = service.RetrieveMultiple((QueryBase)new FetchExpression(query));
            return entityCollection == null || entityCollection.Entities.Count == 0 ? (Entity)null : entityCollection.Entities[0];
        }

        public static Entity RetrieveEntity(
          IOrganizationService service,
          string entityName,
          string[] entitySearchField,
          object[] entitySearchFieldValue,
          ColumnSet columnSet,
          ConditionOperator op)
        {
            Entity entity = (Entity)null;
            QueryExpression queryExpression = new QueryExpression();
            queryExpression.EntityName = entityName;
            FilterExpression filterExpression = new FilterExpression();
            for (int index = 0; index < entitySearchField.Length; ++index)
            {
                ConditionExpression condition = new ConditionExpression();
                condition.AttributeName = entitySearchField[index];
                condition.Operator = op;
                condition.Values.Add(entitySearchFieldValue[index]);
                filterExpression.FilterOperator = LogicalOperator.And;
                filterExpression.AddCondition(condition);
            }
            queryExpression.ColumnSet = columnSet;
            queryExpression.Criteria = filterExpression;
            EntityCollection entityCollection;
            try
            {
                entityCollection = service.RetrieveMultiple((QueryBase)queryExpression);
            }
            catch (Exception ex)
            {
                throw new Exception("Error-RetrieveEntity", ex);
            }
            if (entityCollection.Entities.Count == 1)
                entity = entityCollection.Entities[0];
            return entity;
        }

        public static Guid RegisterStep(
          IOrganizationService service,
          string message,
          PluginStep.PluginStepStage stage,
          string primaryEntityName,
          string primaryAttributes,
          string relatedAttributes)
        {
            string empty = string.Empty;
            string str1 = ConfigHelper.ProcessAttributes(primaryEntityName, primaryAttributes, ref empty, relatedAttributes);
            string str2 = "On" + message + primaryEntityName + Guid.NewGuid().ToString();
            return new PluginStep()
            {
                Message = message,
                PrimaryEntity = primaryEntityName,
                EventHandler = "MLEventHandler.PluginStepHandler",
                Name = str2,
                Rank = 1,
                Stage = stage,
                Mode = PluginStep.PluginStepMode.Synchronous,
                Deployment = PluginStep.PluginStepDeployment.ServerOnly,
                UnsecureConfiguration = str1,
                PreImageAlias = "PreImage",
                PreImageAttributes = empty
            }.RegisterStep(ref service);
        }

        public static void UnRegisterStep(IOrganizationService service, Guid guid)
        {
            service.Delete("sdkmessageprocessingstep", guid);
        }
    }
}
