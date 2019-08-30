namespace L4d2AddonsMgr.AcfFileSpace {

    internal partial class AcfFile {

        /*
         * Begin to miss the much better enum in java.
         * Holy shit no extra data/function holding allowed.
         * https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/enum
         * 
         * This is just too much.
         * https://docs.microsoft.com/en-us/dotnet/standard/microservices-architecture/microservice-ddd-cqrs-patterns/enumeration-classes-over-enum-types
         */
        enum TokenType {
            LstBegin, LstEnd, StringType, NakedString, Comment, Eof, Failure
        }

    }

}
