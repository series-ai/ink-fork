﻿using System.Collections.Generic;
using Ink.Parsed;

namespace Ink
{
	internal partial class InkParser
	{
        protected class FlowDecl
        {
            public string name;
            public List<FlowBase.Argument> arguments;
            public bool isFunction;
        }

		protected Knot KnotDefinition()
		{
            var knotDecl = Parse(KnotDeclaration);
            if (knotDecl == null)
                return null;

			Expect(EndOfLine, "end of line after knot name definition", recoveryRule: SkipToNextLine);

			ParseRule innerKnotStatements = () => StatementsAtLevel (StatementLevel.Knot);

            var content = Expect (innerKnotStatements, "at least one line within the knot", recoveryRule: KnotStitchNoContentRecoveryRule) as List<Parsed.Object>;
			 
            return new Knot (knotDecl.name, content, knotDecl.arguments, knotDecl.isFunction);
		}

        protected FlowDecl KnotDeclaration()
        {
            Whitespace ();

            if (KnotTitleEquals () == null)
                return null;

            Whitespace ();


            string identifier = Parse(Identifier);
            string knotName;

            bool isFunc = identifier == "function";
            if (isFunc) {
                Expect (Whitespace, "whitespace after the 'function' keyword");
                knotName = Expect (Identifier, "the name of the function") as string;
            } else {
                knotName = identifier;
            }

            Whitespace ();

            List<FlowBase.Argument> parameterNames = Parse (BracketedKnotDeclArguments);

            Whitespace ();

            // Optional equals after name
            Parse(KnotTitleEquals);

            return new FlowDecl () { name = knotName, arguments = parameterNames, isFunction = isFunc };
        }

        protected string KnotTitleEquals()
        {
            // 2+ "=" starts a knot
            var multiEquals = ParseCharactersFromString ("=");
            if (multiEquals == null || multiEquals.Length <= 1) {
                return null;
            } else {
                return multiEquals;
            }
        }

		protected object StitchDefinition()
		{
            var decl = Parse(StitchDeclaration);
            if (decl == null)
                return null;

			Expect(EndOfLine, "end of line after stitch name", recoveryRule: SkipToNextLine);

			ParseRule innerStitchStatements = () => StatementsAtLevel (StatementLevel.Stitch);

            var content = Expect(innerStitchStatements, "at least one line within the stitch", recoveryRule: KnotStitchNoContentRecoveryRule) as List<Parsed.Object>;

            return new Stitch (decl.name, content, decl.arguments, decl.isFunction );
		}

        protected FlowDecl StitchDeclaration()
        {
            Whitespace ();

            // Single "=" to define a stitch
            if (ParseString ("=") == null)
                return null;

            // If there's more than one "=", that's actually a knot definition (or divert), so this rule should fail
            if (ParseString ("=") != null)
                return null;

            Whitespace ();

            // Stitches aren't allowed to be functions, but we parse it anyway and report the error later
            bool isFunc = ParseString ("function") != null;
            if ( isFunc ) {
                Whitespace ();
            }

            string stitchName = Parse(Identifier);
            if (stitchName == null)
                return null;

            Whitespace ();

            List<FlowBase.Argument> flowArgs = Parse(BracketedKnotDeclArguments);

            Whitespace ();

            return new FlowDecl () { name = stitchName, arguments = flowArgs, isFunction = isFunc };
        }


		protected object KnotStitchNoContentRecoveryRule()
		{
            // Jump ahead to the next knot or the end of the file
            ParseUntil (KnotDeclaration, new CharacterSet ("="), null);

            var recoveredFlowContent = new List<Parsed.Object>();
			recoveredFlowContent.Add( new Parsed.Text("<ERROR IN FLOW>" ) );
			return recoveredFlowContent;
		}

        protected List<FlowBase.Argument> BracketedKnotDeclArguments()
        {
            if (ParseString ("(") == null)
                return null;

            var flowArguments = Interleave<FlowBase.Argument>(Spaced(FlowDeclArgument), Exclude (String(",")));

            Expect (String (")"), "closing ')' for parameter list");

            // If no parameters, create an empty list so that this method is type safe and 
            // doesn't attempt to return the ParseSuccess object
            if (flowArguments == null) {
                flowArguments = new List<FlowBase.Argument> ();
            }

            return flowArguments;
        }

        protected FlowBase.Argument FlowDeclArgument()
        {
            // Next word could either be "ref" or the argument name
            var firstIden = Parse(Identifier);
            if (firstIden == null)
                return null;

            var flowArg = new FlowBase.Argument ();

            // Passing by reference
            if (firstIden == "ref") {

                Whitespace ();

                flowArg.name = (string) Expect (Identifier, "parameter name after 'ref'");
                flowArg.isByReference = true;

            } 

            // Simple argument name
            else {
                flowArg.name = firstIden;
                flowArg.isByReference = false;
            }

            return flowArg;
        }

	}
}

