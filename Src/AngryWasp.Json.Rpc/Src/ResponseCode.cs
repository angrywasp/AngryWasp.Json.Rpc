namespace AngryWasp.Json.Rpc
{
    //200 = OK
    //400 = Handler error. Error message should be provided as the JSON response
    public enum Response_Code
    {
        OK = 200,
        Error = 400,
    }
}