// Jakar.Extensions :: Jakar.Extensions.Hosting
// 09/22/2022  4:02 PM

namespace Jakar.Database;


public static class Controllers
{
    public const string DETAILS = nameof(DETAILS);
    public const string ERROR   = nameof(ERROR);



    extension( ControllerBase self )
    {
        public ActionResult ClientClosed() => new StatusCodeResult(Status.ClientClosedRequest.AsInt());
        public ActionResult Duplicate( Exception e )
        {
            self.AddError(e);
            return self.ItemNotFound();
        }
        public ActionResult Duplicate( Exception e, params string[] errors )
        {
            self.AddError(e);
            return self.ItemNotFound(errors);
        }
        public ActionResult Duplicate( params string[] errors )
        {
            self.AddError(errors);
            return self.Duplicate();
        }
        public ActionResult Duplicate()
        {
            ModelStateDictionary modelState = self.ModelState;

            return modelState.ErrorCount > 0
                       ? new NotFoundObjectResult(modelState)
                       : self.NotFound();
        }
        public ActionResult FileNotFound( FileNotFoundException e )
        {
            self.AddError(e);
            ModelStateDictionary modelState = self.ModelState;

            return modelState.ErrorCount > 0
                       ? self.NotFound(modelState)
                       : !string.IsNullOrWhiteSpace(e.Message)
                           ? self.NotFound(e.Message)
                           : self.NotFound();
        }
        public ActionResult GetBadRequest( Exception e, params string[] errors )
        {
            self.AddError(e);
            return self.GetBadRequest(errors);
        }
        public ActionResult GetBadRequest( params string[] errors )
        {
            self.AddError(errors);
            return self.GetBadRequest();
        }
        public ActionResult GetBadRequest()
        {
            ModelStateDictionary modelState = self.ModelState;
            Guard.IsGreaterThan(modelState.ErrorCount, 0, nameof(modelState));

            return self.BadRequest(modelState);
        }
        public ActionResult ItemNotFound( Exception e )
        {
            self.AddError(e);
            return self.ItemNotFound();
        }
        public ActionResult ItemNotFound( Exception e, params string[] errors )
        {
            self.AddError(e);
            return self.ItemNotFound(errors);
        }
        public ActionResult ItemNotFound( params string[] errors )
        {
            self.AddError(errors);
            return self.ItemNotFound();
        }
        public ActionResult ItemNotFound()
        {
            ModelStateDictionary modelState = self.ModelState;

            return modelState.ErrorCount > 0
                       ? new NotFoundObjectResult(modelState)
                       : self.NotFound();
        }
        public ActionResult NotAcceptable( Exception e )
        {
            self.AddError(e);
            return self.NotAcceptable();
        }
        public ActionResult NotAcceptable( FormatException e )
        {
            self.AddError(e);
            return self.NotAcceptable();
        }
        public ActionResult NotAcceptable( NotAcceptableException e )
        {
            self.AddError(e);
            return self.NotAcceptable();
        }
        public ActionResult NotAcceptable( string message )
        {
            self.AddError(message);
            return self.NotAcceptable();
        }
        public ActionResult NotAcceptable()
        {
            ModelStateDictionary modelState = self.ModelState;
            Guard.IsGreaterThan(modelState.ErrorCount, 0, nameof(modelState));

            return modelState.ErrorCount > 0
                       ? new UnprocessableEntityObjectResult(modelState) { StatusCode = Status.NotAcceptable.AsInt() }
                       : self.UnprocessableEntity();
        }
        public ActionResult Problem( in Status       status )                    => self.Problem(self.ToProblemDetails(status));
        public ActionResult Problem( ProblemDetails  details )                   => new ObjectResult(details) { StatusCode = details.Status };
        public ActionResult ServerProblem( Exception e )                         => self.ServerProblem(e.Message);
        public ActionResult ServerProblem( string    message = "Unknown Error" ) => self.Problem(message, statusCode: Status.InternalServerError.AsInt());
        public ActionResult TimeoutOccurred( Exception e )
        {
            self.AddError(e);
            return self.NotAcceptable();
        }
        public ActionResult TimeoutOccurred( TimeoutException e )
        {
            self.AddError(e);
            return self.NotAcceptable();
        }
        public ActionResult TimeoutOccurred()
        {
            ModelStateDictionary modelState = self.ModelState;
            Guard.IsGreaterThan(modelState.ErrorCount, 0, nameof(modelState));

            return new ObjectResult(new SerializableError(modelState)) { StatusCode = Status.GatewayTimeout.AsInt() };
        }
        public ActionResult UnauthorizedAccess( Exception e )
        {
            self.AddError(e);
            ModelStateDictionary modelState = self.ModelState;

            return modelState.ErrorCount > 0
                       ? self.UnprocessableEntity(modelState)
                       : string.IsNullOrWhiteSpace(e.Message)
                           ? self.UnprocessableEntity(e.Message)
                           : self.UnprocessableEntity();
        }
        public ActionResult UnauthorizedAccess( UnauthorizedAccessException e )
        {
            self.AddError(e);
            ModelStateDictionary modelState = self.ModelState;

            return modelState.ErrorCount > 0
                       ? self.UnprocessableEntity(modelState)
                       : string.IsNullOrWhiteSpace(e.Message)
                           ? self.UnprocessableEntity(e.Message)
                           : self.UnprocessableEntity();
        }
        public ActionResult UnauthorizedAccess()
        {
            ModelStateDictionary modelState = self.ModelState;

            return modelState.ErrorCount > 0
                       ? self.UnprocessableEntity(modelState)
                       : self.UnprocessableEntity();
        }
        public ProblemDetails ToProblemDetails( in Status status ) => self.ModelState.ToProblemDetails(status);
    }



    extension( ControllerBase self )
    {
        public void AddError( string              error, string? title = null, string key = ERROR ) { self.ModelState.AddError(error, title, key); }
        public void AddError( params string[]     errors ) { self.ModelState.AddError(errors.AsSpan()); }
        public void AddError( IEnumerable<string> errors ) { self.ModelState.AddError(errors); }
        public void AddError( Exception           e )      { self.ModelState.AddError(e); }
    }



    extension( ModelStateDictionary self )
    {
        public void AddError( Exception e )
        {
            self.AddError(e.Message);
            foreach ( DictionaryEntry pair in e.Data ) { self.AddError($"{pair.Key}: {pair.Value}"); }
        }
        public void AddError( string error, string? title = null, string key = ERROR )
        {
            self.AddError(key,
                                title is null
                                    ? error
                                    : $"{title} : {error}");
        }
        public void AddError( scoped in ReadOnlySpan<string> errors )
        {
            if ( errors.IsEmpty ) { return; }

            foreach ( string error in errors ) { self.AddError(error); }
        }
        public void AddError( IEnumerable<string> errors )
        {
            foreach ( string error in errors ) { self.AddError(error); }
        }


        public ProblemDetails ToProblemDetails( in Status status )
        {
            self.TryGetValue(nameof(ProblemDetails.Detail),   out ModelStateEntry? detail);
            self.TryGetValue(nameof(ProblemDetails.Instance), out ModelStateEntry? instance);
            self.TryGetValue(nameof(ProblemDetails.Title),    out ModelStateEntry? title);
            self.TryGetValue(nameof(ProblemDetails.Type),     out ModelStateEntry? type);

            ProblemDetails problem = new()
                                     {
                                         Detail   = detail?.AttemptedValue,
                                         Instance = instance?.AttemptedValue,
                                         Title    = title?.AttemptedValue,
                                         Type     = type?.AttemptedValue,
                                         Status   = status.AsInt()
                                     };

            foreach ( ( string key, ModelStateEntry value ) in self )
            {
                if ( key.IsOneOf(nameof(ProblemDetails.Detail), nameof(ProblemDetails.Instance), nameof(ProblemDetails.Title), nameof(ProblemDetails.Type)) ) { continue; }

                problem.Extensions.Add(key, value.AttemptedValue);
            }

            return problem;
        }
    }
}
