Библиотека использует [Dapper](https://github.com/StackExchange/Dapper).

Зачем нужна эта библиотека?
===
* Простое API для склейки запросов близкое к `StringBuilder`. Метод `Append`.
* Автоматическая генерация классов для данных.
* Автоматические проверки. Все поля из запроса присутствуют в классе. Типы полей в запросе совпадают с типами свойств в классе.

Склейка запросов
===
```csharp
        Task<List<PostInfo>> GetPosts(DateTime? date)
        {
            var query = new Query(@"
SELECT p.PostId, p.Text, p.CreationDate
FROM Post p
WHERE 1 = 1");
            if (date.HasValue)
                query.Append(@"
    AND p.CreationDate >= @date", new {date});

            return query.ToList<PostInfo>(Db);
        }
```

Подзапросы
===
```csharp
        public Task<List<LetterInfo>> GetLetters()
        {
            var query = new Query();
            query.Append($@"
SELECT l.LetterId, l.Subject
FROM ({Letter(query)}) l
ORDER BY l.LetterId");

            return query.ToList<LetterInfo>(_db);
        }
        
        private Query Letter(Query query)
        {
            return query.Query(@"
SELECT * 
FROM Letter l
WHERE l.ClientId = @ClientId
", new {_currentUser.ClientId});
        }
```