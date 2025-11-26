// Jakar.Extensions :: Jakar.Database
// 03/06/2023  12:59 AM


namespace Jakar.Database;


public static class ErrorOrResults
{
    public static readonly BadRequest             BadRequest          = TypedResults.BadRequest();
    public static readonly Conflict               Conflict            = TypedResults.Conflict();
    public static readonly NoContent              NoContent           = TypedResults.NoContent();
    public static readonly NotFound               NotFound            = TypedResults.NotFound();
    public static readonly Ok                     Ok                  = TypedResults.Ok();
    public static readonly UnauthorizedHttpResult Unauthorized        = TypedResults.Unauthorized();
    public static readonly UnprocessableEntity    UnprocessableEntity = TypedResults.UnprocessableEntity();


    public static JsonResult<TValue> ToJsonResult<TValue>( this TValue value, Status status = Status.Ok ) => JsonResult<TValue>.Create(value, status);


    public static bool IsAuthorized( this ClaimsPrincipal principal, RecordID<UserRecord> id ) => principal.IsAuthorized(id.Value);
    public static bool IsAuthorized( this ClaimsIdentity  principal, RecordID<UserRecord> id ) => principal.IsAuthorized(id.Value);



    extension<TValue>( ErrorOrResult<TValue> result )
    {
        public SerializableError? ToSerializableError() => result.TryGetValue(out Errors? errors)
                                                               ? new SerializableError(errors.ToModelStateDictionary())
                                                               : null;

        public ModelStateDictionary? ToModelStateDictionary() => result.TryGetValue(out Errors? errors)
                                                                     ? errors.ToModelStateDictionary()
                                                                     : null;
    }



    extension( Errors errors )
    {
        public ObjectResult       ToActionResult()      => new(errors) { StatusCode = (int)errors.GetStatus() };
        public JsonResult<Errors> ToResult()            => JsonResult<Errors>.Create(errors, errors.GetStatus());
        public JsonResult<Errors> ToJsonResult()        => JsonResult<Errors>.Create(errors, errors.GetStatus());
        public SerializableError  ToSerializableError() => new(errors.ToModelStateDictionary());

        public ModelStateDictionary ToModelStateDictionary()
        {
            ModelStateDictionary state = new();
            foreach ( Error error in errors.Details.AsSpan() ) { state.Add(error); }

            return state;
        }
    }



    extension( Error error )
    {
        public ObjectResult       ToActionResult() => new(error) { StatusCode = (int)error.StatusCode };
        public JsonResult<Errors> ToResult()       => JsonResult<Errors>.Create(Errors.Create([error]), error.StatusCode);
        public JsonResult<Errors> ToJsonResult()   => JsonResult<Errors>.Create(Errors.Create([error]), error.StatusCode);

        public ModelStateDictionary ToModelStateDictionary()
        {
            ModelStateDictionary state = new() { error };
            return state;
        }
    }



    extension( ModelStateDictionary state )
    {
        public void Add( Errors errors )
        {
            foreach ( Error error in errors.Details.AsSpan() ) { state.Add(error); }
        }

        public void Add( Error error )
        {
            if ( !error.Details.IsEmpty )
            {
                StringTags tags = error.Details;
                foreach ( string e in tags.Entries.AsSpan() ) { state.TryAddModelError(nameof(Errors), e); }

                foreach ( ref readonly Pair e in tags.Tags.AsSpan() ) { state.TryAddModelError(e.Key, e.Value ?? NULL); }
            }

            state.TryAddModelError(nameof(Error.StatusCode),  error.StatusCode.ToString());
            state.TryAddModelError(nameof(Error.Description), error.Description ?? NULL);
            state.TryAddModelError(nameof(Error.Type),        error.Type        ?? NULL);
            state.TryAddModelError(nameof(Error.Title),       error.Title       ?? NULL);
            state.TryAddModelError(nameof(Error.Instance),    error.Instance    ?? NULL);
        }
    }



    public static async Task<Results<JsonResult<TValue>, JsonResult<Errors>>> ToResult<TValue>( this Task<ErrorOrResult<TValue>> result )
    {
        ErrorOrResult<TValue> errorOrResult = await result.ConfigureAwait(false);
        return errorOrResult.ToResult();
    }
    public static async ValueTask<Results<JsonResult<TValue>, JsonResult<Errors>>> ToResult<TValue>( this ValueTask<ErrorOrResult<TValue>> result )
    {
        ErrorOrResult<TValue> errorOrResult = await result.ConfigureAwait(false);
        return errorOrResult.ToResult();
    }



    extension<TValue>( ErrorOrResult<TValue> result )
    {
        public Results<JsonResult<TValue>, JsonResult<Errors>> ToResult() =>
            result.TryGetValue(out TValue? value, out Errors? errors)
                ? JsonResult<TValue>.Create(value, Errors.GetStatus(errors))
                : errors.ToResult();

        public ActionResult<TValue> ToActionResult() => result.TryGetValue(out TValue? value, out Errors? errors)
                                                            ? new ObjectResult(value) { StatusCode  = (int)Errors.GetStatus(errors) }
                                                            : new ObjectResult(errors) { StatusCode = (int)errors.GetStatus() };
    }



    public static async Task<ActionResult<TValue>> ToActionResult<TValue>( this Task<ErrorOrResult<TValue>> result )
    {
        ErrorOrResult<TValue> errorOrResult = await result.ConfigureAwait(false);
        return errorOrResult.ToActionResult();
    }
    public static async ValueTask<ActionResult<TValue>> ToActionResult<TValue>( this ValueTask<ErrorOrResult<TValue>> result )
    {
        ErrorOrResult<TValue> errorOrResult = await result.ConfigureAwait(false);
        return errorOrResult.ToActionResult();
    }
}
