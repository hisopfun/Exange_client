using System;


public class TicksEventArgs : EventArgs { 
    public string tick { get; set; }
}
class TickEncoder
{
    public delegate void TickEncoderEventHandler(object source, TicksEventArgs args);
    public event TickEncoderEventHandler TickEncoded;

    public void Encode(string str) {
        OnTickEncoded(str);
    }

    protected virtual void OnTickEncoded(string str) {
        if (TickEncoded != null)
            TickEncoded(this, new TicksEventArgs() { tick = str});
    }
}


public class OrderEventArgs : EventArgs
{
    public string order { get; set; }
}
public class OrderEncoder
{
    public delegate void OrderEncoderEventHandler(object source, OrderEventArgs args);
    public event OrderEncoderEventHandler OrderEncoded;

    public void Encode(string str)
    {
        OnOrderEncoded(str);
    }

    protected virtual void OnOrderEncoded(string str)
    {
        if (OrderEncoded != null)
            OrderEncoded(this, new OrderEventArgs() { order = str });
    }
}


public class ReportEventArgs : EventArgs
{
    public string Report { get; set; }
}
public class ReportEncoder
{
    public delegate void ReportEncoderEventHandler(object source, ReportEventArgs args);
    public event ReportEncoderEventHandler ReportEncoded;

    public void Encode(string str)
    {
        OnReportEncoded(str);
    }

    protected virtual void OnReportEncoded(string str)
    {
        if (ReportEncoded != null)
            ReportEncoded(this, new ReportEventArgs() { Report = str });
    }
}


public class QuoteEventArgs : EventArgs
{
    public string Quote { get; set; }
}
public class QuoteEncoder
{
    public delegate void QuoteEncoderEventHandler(object source, QuoteEventArgs args);
    public event QuoteEncoderEventHandler QuoteEncoded;

    public void Encode(string str)
    {
        OnQuoteEncoded(str);
    }

    protected virtual void OnQuoteEncoded(string str)
    {
        if (QuoteEncoded != null)
            QuoteEncoded(this, new QuoteEventArgs() { Quote = str });
    }
}

