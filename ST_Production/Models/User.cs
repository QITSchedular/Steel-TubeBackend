﻿namespace ST_Production.Models
{
    public class User
    {
        public int User_ID { get; set; } = int.MinValue;
        public string User_Name { get; set; }
        public string User_Email { get; set; } = string.Empty;
        public string User_Password { get; set; }
        public long Mobile_No { get; set; }
        public string Profile_Picture { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public byte[]? ProfilePicture { get; set; }

    }
    public class getDataClass
    {
        public int User_ID { get; set; }
        public List<getModuleClass> moduleCLasses { get; set; }
    }

    public class getModuleClass
    {
        public string text { get; set; }
        public string path { get; set; }
        public string icon { get; set; }
        public bool hasAccess { get; set; }
        public List<getSubModuleClass> items { get; set; }

    }

    public class getSubModuleClass
    {
        public string text { get; set; }
        public string path { get; set; }
        public string icon { get; set; }
        public List<string> rightsAccess { get; set; }
    }

    public class getUserAuthRule
    {
        public int User_ID { get; set; }
    }

    public class EditUser
    {
        public string User_Name { get; set; }
        public string User_Email { get; set; } = string.Empty;
        public long Mobile_No { get; set; }
        public string Profile_Picture { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public byte[]? ProfilePicture { get; set; }
        public string? Old_Password { get; set; } = string.Empty;
        public string? New_Password { get; set; } = string.Empty;
    }

    public class ChangePassword
    {
        public string User_Name { get; set; }
        public string User_Password { get; set; }
    }

}
