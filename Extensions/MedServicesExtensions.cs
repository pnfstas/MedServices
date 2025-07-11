using MedServices.Models;
using MedServices.Startup;
using Microsoft.AspNetCore.Http;
using NETCore.MailKit.Infrastructure.Internal;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace MedServices.Extensions
{
    public static class TypeExtensions
    {
        public static string GetMemberDescription(this Type type, string strMemberName)
        {
            MemberInfo memberInfo =
                (from curMemberInfo in type?.GetMember(strMemberName)
                 where string.Equals(curMemberInfo?.Name, strMemberName)
                 select curMemberInfo).FirstOrDefault();
            string description = null;
            try
            {
                description =
                    (from curConstructorArguments in
                    (from curCustomAttributeData in CustomAttributeData.GetCustomAttributes(memberInfo)
                     where curCustomAttributeData?.AttributeType == typeof(DescriptionAttribute)
                         && curCustomAttributeData.ConstructorArguments?.Count > 0
                     select curCustomAttributeData.ConstructorArguments).FirstOrDefault()
                     select curConstructorArguments.Value as string).FirstOrDefault();
            }
            catch(Exception e)
            {
                Debug.WriteLine(e);
                throw;
            }
            return description;
        }
        public static Dictionary<string, string> GetCustomAttributeValues(this Type type, Type attributeType, string? strNamedArgumentName = null)
        {
            PropertyInfo[] arrPropInfo = type.GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
            Dictionary<string, string> dictAttributeValues = new Dictionary<string, string>
                (from CurPropInfo in arrPropInfo
                 from CurCustomAttrData in CurPropInfo.GetCustomAttributesData()
                 where CurCustomAttrData.AttributeType == attributeType
                 from CurNamedArg in CurCustomAttrData.NamedArguments
                 where string.IsNullOrWhiteSpace(strNamedArgumentName) || string.Equals(CurNamedArg.MemberName, strNamedArgumentName)
                 select new KeyValuePair<string, string>(CurPropInfo.Name, CurNamedArg.TypedValue.Value as string));
            return dictAttributeValues;
        }
    }

    public static class ObjectExtensions
    {
        public static void CopyPropertiesFrom(this object target, object? source, string? excludes = null)
        {
            if(source != null)
            {
                IEnumerable<string> arrExcludes = excludes?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                Dictionary<string, PropertyInfo> dictTargetPropInfo = new Dictionary<string, PropertyInfo>
                    (from CurPropInfo in target.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                     where CurPropInfo?.SetMethod?.IsPublic == true && CurPropInfo.SetMethod.IsFinal && arrExcludes?.Contains(CurPropInfo.Name) != true
                     select new KeyValuePair<string, PropertyInfo>(CurPropInfo.Name, CurPropInfo)
                    );
                IEnumerable<PropertyInfo> sourcePropInfo =
                    from CurPropInfo in source.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    where CurPropInfo?.GetMethod?.IsPublic == true && CurPropInfo.GetMethod.IsFinal && dictTargetPropInfo.ContainsKey(CurPropInfo.Name)
                    select CurPropInfo;
                PropertyInfo curTargetPropInfo = null;
                object curSource = null;
                object curTarget = null;
                object curValue = null;
                foreach(PropertyInfo curSourcePropInfo in sourcePropInfo)
                {
                    curTargetPropInfo = dictTargetPropInfo[curSourcePropInfo.Name];
                    curSource = curSourcePropInfo.GetMethod.IsStatic ? null : source;
                    curTarget = curTargetPropInfo.SetMethod.IsStatic ? null : target;
                    curValue = curSourcePropInfo.GetValue(curSource);
                    curTargetPropInfo.SetValue(curTarget, curValue);
                }
            }
        }
        public static void CopyPropertiesFromFormCollection(this object trgObject, IFormCollection? srcCollection, string? excludes = null)
        {
            if(srcCollection?.Count > 0)
            {
                IEnumerable<string> arrExcludes = excludes?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                Dictionary<string, PropertyInfo> dictTargetPropInfo = new Dictionary<string, PropertyInfo>
                    (from CurPropInfo in trgObject.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                     where CurPropInfo?.SetMethod?.IsPublic == true 
                        //&& CurPropInfo.SetMethod.IsFinal 
                        && arrExcludes?.Contains(CurPropInfo.Name) != true
                     select new KeyValuePair<string, PropertyInfo>(CurPropInfo.Name, CurPropInfo)
                    );
                IEnumerable<string> sourceKeys =
                    from CurKey in srcCollection.Keys
                    where dictTargetPropInfo.ContainsKey(CurKey)
                    select CurKey;
                PropertyInfo curTargetPropInfo = null;
                object curTarget = null;
                object objValue = null;
                string strValue = null;
                bool isConverted = false;
                foreach(string curKey in sourceKeys)
                {
                    curTargetPropInfo = dictTargetPropInfo[curKey];
                    curTarget = curTargetPropInfo.SetMethod.IsStatic ? null : trgObject;
                    strValue = srcCollection[curKey].ToString();
                    isConverted = false;
                    if(curTargetPropInfo.PropertyType == typeof(string))
                    {
                        objValue = strValue;
                        isConverted = true;
                    }
                    else if(curTargetPropInfo.PropertyType.IsEnum)
                    {
                        isConverted = Enum.TryParse(curTargetPropInfo.PropertyType, strValue, out objValue);
                    }
                    else
                    {
                        TypeConverter typeConverter = TypeDescriptor.GetConverter(curTargetPropInfo.PropertyType);
                        if(typeConverter.CanConvertFrom(typeof(string)))
                        {
                            //objValue = Convert.ChangeType(strValue, curTargetPropInfo.PropertyType);
                            objValue = typeConverter.ConvertFromString(strValue);
                            isConverted = true;
                        }
                    }
                    if(isConverted)
                    {
                        curTargetPropInfo.SetValue(curTarget, objValue);
                    }
                }
            }
        }
		public static string ToQueryString(this object obj)
        {
            JObject jobj = null;
            try
            {
                jobj = JObject.FromObject(obj);
            }
            catch(Exception e)
            {
                Debug.WriteLine($"{e}");
                throw;
            }
            string strQueryString = jobj?.Properties()?.Count() > 0 ?
                string.Join('&',
                    from curProperty in jobj?.Properties()
                    let jobjValue = JObject.FromObject(curProperty?.Value)
                    let strValue = jobjValue?.Properties()?.Count() > 0 ? jobjValue.ToString() : curProperty?.Value?.ToString()
                    where !string.IsNullOrWhiteSpace(curProperty?.Name) && !string.IsNullOrWhiteSpace(strValue)
                    select $"{curProperty.Name}={HttpUtility.UrlEncode(strValue)}") : obj?.ToString();
            return strQueryString;
        }
    }

    public static class EnumExtensions
    {
        public static string GetValueDescription(this Enum value)
        {
            Type type = value?.GetType();
            return type?.GetMemberDescription(Enum.GetName(type, value));
        }
    }

    public static class MailKitOptionsExtensions
    {
        public static MailKitOptions FillFromConfiguration(this MailKitOptions options)
        {
            Startup.Startup.AppConfiguration?.GetSection(MailKitServiceOptions.SectionName)?.Bind(options);
            return options;
        }
    }
    
    public static class ModelExtension
    {
        public static Dictionary<string, string> GetDisplayNames(this ModelBase model)
        {
            return model?.GetType()?.GetCustomAttributeValues(typeof(DisplayAttribute), "Name");
        }
        public static Dictionary<string, string> GetErrorDescriptions(this ModelBase model)
        {
            return model?.GetType()?.GetCustomAttributeValues(typeof(RequiredAttribute), "ErrorMessage");
        }
    }
}
