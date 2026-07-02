using System.Collections.Generic;

namespace AutomationApp.Configuration
{
    public class AppSettings
    {
        public PathSettings Paths {get; set;} =new();
        public bool DebugMode {get; set;} = new();
        public BethaSettings Betha {get; set;}= new();
        public SocSettings Soc {get; set;} = new();
        public ProxySettings Proxy {get; set;} = new();
        public GoogleSheetsSettings GoogleSheets {get; set;} = new();

    }

    public class PathSettings
    {
        public string Workspace {get; set;} = string.Empty;
        public string AsosDownloads {get; set;} = string.Empty;
        public string Reports {get; set;} = string.Empty;
    }

    public class BethaSettings 
        {
            public BethaApiSettings Api {get; set;} = new();
            public EsocialSettings Esocial {get; set;} = new();
            public EncryptedCredentials Credentials {get; set;} = new();
        }

    public class BethaApiSettings
    {
        public string BaseUrl {get; set;} = string.Empty;
        public string LoginUrl {get; set;} = string.Empty;
        public string UserAccess {get; set;} = string.Empty;
        public string Authorization {get; set;} = string.Empty;
        public long  MedicalInstitutionId {get; set;}
        public long AsoFormId {get; set;}
        public string FormFieldType {get; set;} = string.Empty;
        public Dictionary<string, string> Endpoints {get;set;} = new();
    }

    public class EsocialSettings
    {
        public string BaseUrl {get; set;} = string.Empty;
        public string UserAccess {get; set;} = string.Empty;
        public Dictionary<string, string> Endpoints {get; set;} = new();
    }

    public class SocSettings
    {
        public string BaseUrl {get; set;} = string.Empty;
        public EncryptedCredentials Credentials {get; set;} = new();
        
    }

    public class EncryptedCredentials
    {
        public string Login {get; set;} = string.Empty;
        public string Password {get; set;} = string.Empty;
        public string VirtualPassword {get;set;} = string.Empty;
    }

    public class ProxySettings
    {
        public string Host {get; set;} = string.Empty;
        public string Port {get; set;} = string.Empty;
        public string Username {get; set;} = string.Empty;
        public string Password {get; set;} = string.Empty;
    }

    public class GoogleSheetsSettings
    {
        public string SpreadsheetName {get;set;} = string.Empty;
        public string TabName {get;set;} = string.Empty;

    }
}