﻿using System;
using System.Linq;

namespace Keeper.BacktraQ
{
    public class Phrase
    {
        private Func<Var<VarList<char>>, Var<VarList<char>>, Query> function;

        public Phrase(Func<Var<VarList<char>>, Var<VarList<char>>, Query> function)
        {
            this.function = function;
        }

        public Query AsString(Var<string> text)
        {
            var textChars = new Var<VarList<char>>();

            return Query.IfThen(text.IsNonVar(), text <= textChars.AsString)
                            & this.BuildQuery(textChars, null)
                            & text <= textChars.AsString;
        }

        public Query BuildQuery(Var<VarList<char>> text, Var<VarList<char>> tail = null)
        {
            tail = tail ?? VarList.Create("");

            return function(text, tail);
        }

        public static implicit operator Phrase(Func<Var<VarList<char>>, Var<VarList<char>>, Query> function)
        {
            return new Phrase(function);
        }

        public static implicit operator Phrase(string token)
        {
            return Token(VarList.Create(token));
        }

        public static implicit operator Phrase(Var<VarList<char>> token)
        {
            return Token(token);
        }

        public static implicit operator Phrase(Query query)
        {
            return new Phrase((x, y) => x <= y & query);
        }

        public static Phrase Token(Var<VarList<char>> token)
        {
            return new Phrase((text, tail) =>
            {
                text = text ?? new Var<VarList<char>>();
                tail = tail ?? new Var<VarList<char>>();

                return token.Append(tail, text);
            });
        }

        public static Tuple<T, Phrase> SwitchCase<T>(T value, Phrase phrase)
        {
            return Tuple.Create(value, phrase);
        }

        public static Phrase SwitchPhrase<T>(Var<T> variable, params Tuple<T, Phrase>[] cases)
        {
            return OptionPhrase(cases.Select(switchCase => ChainPhrase(variable <= switchCase.Item1, switchCase.Item2)).ToArray());
        }

        public static Phrase OptionPhrase(params Phrase[] elements)
        {
            return new Phrase((text, tail) =>
            {
                var elementQueries = elements.Select(element => element.BuildQuery(text, tail));

                return Query.Any(elementQueries);
            });
        }

        public static Phrase RandomPhrase(params Phrase[] elements)
        {
            return new Phrase((text, tail) =>
            {
                var elementQueries = elements.Select(element => element.BuildQuery(text, tail)).ToArray();

                var elementList = VarList.Create(elementQueries);
                var elementVar = new Var<Query>();

                return elementVar <= elementList.RandomMember & elementVar.Value;
            });
        }

        public static Phrase ChainPhrase(params Phrase[] elements)
        {
            return new Phrase((text, tail) =>
            {
                var previousTail = text;
                Var<VarList<char>> nextTail = null;
                Query result = null;

                for (int elementIndex = 0; elementIndex < elements.Length; elementIndex++)
                {
                    if (elementIndex + 1 == elements.Length)
                    {
                        nextTail = tail;
                    }
                    else
                    {
                        nextTail = new Var<VarList<char>>();
                    }

                    if (result == null)
                    {
                        result = elements[elementIndex].BuildQuery(previousTail, nextTail);
                    }
                    else
                    {
                        result = result & elements[elementIndex].BuildQuery(previousTail, nextTail);
                    }

                    previousTail = nextTail;
                }

                return result;
            });
        }

        public static Phrase operator +(Phrase left, Phrase right)
        {
            return new Phrase((text, tail) =>
            {
                var mid = new Var<VarList<char>>();

                return left.BuildQuery(text, mid)
                        & right.BuildQuery(mid, tail);
            });
        }

        public static Phrase operator ^(Phrase left, Phrase right)
        {
            return new Phrase((text, tail) =>
            {
                return left.BuildQuery(text, tail)
                        | right.BuildQuery(text, tail);
            });
        }
    }
}
