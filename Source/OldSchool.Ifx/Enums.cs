namespace OldSchool.Ifx
{
    internal enum TelnetResponseCode
    {
        /// <summary>The "will" telnet response code.</summary>
        Will = 251,

        /// <summary>The "won't" telnet response code.</summary>
        Wont = 252,

        /// <summary>The "do" telnet response code.</summary>
        Do = 253,

        /// <summary>The "don't" telnet response code.</summary>
        Dont = 254
    }
}