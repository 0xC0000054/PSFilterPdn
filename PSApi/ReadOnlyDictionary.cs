/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// Represents a read-only collection of keys and values.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
    /// <seealso cref="System.Collections.Generic.IDictionary{TKey, TValue}"/>
    [DataContract]
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    public sealed class ReadOnlyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
#pragma warning disable IDE0044 // Add readonly modifier
        [DataMember]
        private IDictionary<TKey, TValue> dictionary;
#pragma warning restore IDE0044 // Add readonly modifier
        [NonSerialized]
        private KeyCollection keys;
        [NonSerialized]
        private ValueCollection values;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadOnlyDictionary{TKey, TValue}"/> class.
        /// </summary>
        /// <param name="dictionary">The dictionary to wrap.</param>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is null.</exception>
        public ReadOnlyDictionary(IDictionary<TKey, TValue> dictionary)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            this.dictionary = dictionary;
            keys = null;
            values = null;
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="ReadOnlyDictionary{TKey, TValue}"/>.
        /// </summary>
        public int Count => dictionary.Count;

        /// <summary>
        /// Gets a collection containing the keys in the dictionary.
        /// </summary>
        public KeyCollection Keys
        {
            get
            {
                if (keys == null)
                {
                    keys = new KeyCollection(dictionary.Keys);
                }

                return keys;
            }
        }

        /// <summary>
        /// Gets a collection containing the values in the dictionary.
        /// </summary>
        public ValueCollection Values
        {
            get
            {
                if (values == null)
                {
                    values = new ValueCollection(dictionary.Values);
                }

                return values;
            }
        }

        /// <summary>
        /// Gets the value with the specified key.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public TValue this[TKey key] => dictionary[key];

        /// <summary>
        /// Determines whether the <see cref="ReadOnlyDictionary{TKey, TValue}"/> contains an element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the <see cref="ReadOnlyDictionary{TKey, TValue}"/>.</param>
        /// <returns>
        /// true if the <see cref="ReadOnlyDictionary{TKey, TValue}" /> contains an element with the key; otherwise, false.
        /// </returns>
        public bool ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="ReadOnlyDictionary{TKey, TValue}"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">
        /// When this method returns, the value associated with the specified key, if the key is found;
        /// otherwise, the default value for the type of the <paramref name="value" /> parameter.
        /// This parameter is passed uninitialized.
        /// </param>
        /// <returns>
        /// true if the object that implements <see cref="ReadOnlyDictionary{TKey, TValue}"/> contains an element with the specified key; otherwise, false.
        /// </returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            return dictionary.TryGetValue(key, out value);
        }

        private static void ThrowNotSupportedException()
        {
            throw new NotSupportedException("The dictionary is read only.");
        }

        #region ICollection<KeyValuePair<TKey, TValue>> Methods
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => true;

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            ThrowNotSupportedException();
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Clear()
        {
            ThrowNotSupportedException();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return dictionary.Contains(item);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            dictionary.CopyTo(array, arrayIndex);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            ThrowNotSupportedException();
            return false;
        }
        #endregion

        #region IDictionary<TKey, TValue> Methods
        ICollection<TKey> IDictionary<TKey, TValue>.Keys => new KeyCollection(dictionary.Keys);

        ICollection<TValue> IDictionary<TKey, TValue>.Values => new ValueCollection(dictionary.Values);

        int ICollection<KeyValuePair<TKey, TValue>>.Count => dictionary.Count;

        TValue IDictionary<TKey, TValue>.this[TKey key]
        {
            get => dictionary[key];
            set => ThrowNotSupportedException();
        }

        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            ThrowNotSupportedException();
        }

        bool IDictionary<TKey, TValue>.ContainsKey(TKey key)
        {
            return dictionary.ContainsKey(key);
        }

        bool IDictionary<TKey, TValue>.Remove(TKey key)
        {
            ThrowNotSupportedException();
            return false;
        }

        bool IDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value)
        {
            return dictionary.TryGetValue(key, out value);
        }
        #endregion

        #region IEnumerator Methods
        IEnumerator IEnumerable.GetEnumerator()
        {
            return dictionary.GetEnumerator();
        }
        #endregion

        /// <summary>
        /// Represents a read-only collection of keys in the <see cref="ReadOnlyDictionary{TKey, TValue}"/>
        /// </summary>
        /// <seealso cref="System.Collections.Generic.ICollection{T}" />
        [DebuggerDisplay("Count = {Count}")]
        [Serializable]
        public sealed class KeyCollection : ICollection<TKey>
        {
            private readonly ICollection<TKey> items;

            public KeyCollection(ICollection<TKey> items)
            {
                this.items = items;
            }

            /// <summary>
            /// Gets the number of elements contained in the collection.
            /// </summary>
            public int Count => items.Count;

            /// <summary>
            /// Determines whether the collection contains a specific value.
            /// </summary>
            /// <param name="item">The object to locate in the collection.</param>
            /// <returns>
            /// true if <paramref name="item" /> is found in the collection; otherwise, false.
            /// </returns>
            public bool Contains(TKey item)
            {
                return items.Contains(item);
            }

            /// <summary>
            /// Copies the elements of the collection to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
            /// </summary>
            /// <param name="array">
            /// The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from collection.
            /// The <see cref="T:System.Array" /> must have zero-based indexing.
            /// </param>
            /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
            public void CopyTo(TKey[] array, int arrayIndex)
            {
                items.CopyTo(array, arrayIndex);
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
            /// </returns>
            public IEnumerator<TKey> GetEnumerator()
            {
                return items.GetEnumerator();
            }

            bool ICollection<TKey>.IsReadOnly => true;

            void ICollection<TKey>.Add(TKey item)
            {
                ThrowNotSupportedException();
            }

            void ICollection<TKey>.Clear()
            {
                ThrowNotSupportedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return items.GetEnumerator();
            }

            bool ICollection<TKey>.Remove(TKey item)
            {
                ThrowNotSupportedException();
                return false;
            }
        }

        /// <summary>
        /// Represents a read-only collection of values in the <see cref="ReadOnlyDictionary{TKey, TValue}"/>
        /// </summary>
        /// <seealso cref="System.Collections.Generic.ICollection{T}" />
        [DebuggerDisplay("Count = {Count}")]
        [Serializable]
        public sealed class ValueCollection : ICollection<TValue>
        {
            private readonly ICollection<TValue> items;

            public ValueCollection(ICollection<TValue> items)
            {
                this.items = items;
            }

            /// <summary>
            /// Gets the number of elements contained in the collection.
            /// </summary>
            public int Count => items.Count;

            /// <summary>
            /// Determines whether the collection contains a specific value.
            /// </summary>
            /// <param name="item">The object to locate in the collection.</param>
            /// <returns>
            /// true if <paramref name="item" /> is found in the collection; otherwise, false.
            /// </returns>
            public bool Contains(TValue item)
            {
                return items.Contains(item);
            }

            /// <summary>
            /// Copies the elements of the collection to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
            /// </summary>
            /// <param name="array">
            /// The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from collection.
            /// The <see cref="T:System.Array" /> must have zero-based indexing.
            /// </param>
            /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
            public void CopyTo(TValue[] array, int arrayIndex)
            {
                items.CopyTo(array, arrayIndex);
            }

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
            /// </returns>
            public IEnumerator<TValue> GetEnumerator()
            {
                return items.GetEnumerator();
            }

            bool ICollection<TValue>.IsReadOnly => true;

            void ICollection<TValue>.Add(TValue item)
            {
                ThrowNotSupportedException();
            }

            void ICollection<TValue>.Clear()
            {
                ThrowNotSupportedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return items.GetEnumerator();
            }

            bool ICollection<TValue>.Remove(TValue item)
            {
                ThrowNotSupportedException();
                return false;
            }
        }
    }
}
