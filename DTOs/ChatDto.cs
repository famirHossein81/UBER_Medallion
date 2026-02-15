namespace crud.DTOs
{
    public class ChatRequestDto
    {
        public string Question {get;set;}
    }

    public class ChatResponseDto
    {
        public string GeneratedSql {get;set;}
        public object Answer {get;set;}

        public string Error {get;set;}
    }
}