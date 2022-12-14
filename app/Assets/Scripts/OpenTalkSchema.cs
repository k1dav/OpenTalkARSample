
public class TextMixin
{
    public string text { get; set; }
}

public class TypeValueMixin
{
    public string type { get; set; }
    public string value { get; set; }
}

// Entry
public class EntryReq
{
    public string text { get; set; }
    public string type { get; set; }
}

public class EntryResult
{
#nullable enable
    public string? id { get; set; }
    public string? index_name { get; set; }
#nullable disable
    public TextMixin[] receive { get; set; }
    public TypeValueMixin[] send { get; set; }
}

public class Entry
{
#nullable enable
    public string? debug_msg { get; set; }
    public string? message { get; set; }
    public string? skill_name { get; set; }
#nullable disable
    public bool is_live { get; set; }
    public EntryResult[] result { get; set; }
}

public class EntryData
{
    public Entry entry { get; set; }
}


public class EntryResp
{
    public EntryData data { get; set; }
}


// Virtual Conf
public class VirutalConf
{
#nullable enable
    public string? character { get; set; }
#nullable disable
}
public class VirutalConfData
{
    public VirutalConf[] virtual_idol_conf { get; set; }
}
public class VirtualConfResp
{
    public VirutalConfData data { get; set; }
}

// Response
public class DeviceResponse
{
    public string response_type { get; set; }
    public TypeValueMixin[] messages { get; set; }
}
public class DeviceResponseData
{
    public DeviceResponse[] device_response { get; set; }
}
public class DeviceResponseResp
{
    public DeviceResponseData data { get; set; }
}