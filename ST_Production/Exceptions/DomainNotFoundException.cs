using ST_Production.Exceptions;

namespace ST_Production.Middlewares
{
    public class DomainNotFoundException : DomainException
    {
        public DomainNotFoundException(string message) : base(message)
        {

        }
    }
}
