// Copyright (c) 2018 Shachar Langbeheim. All rights reserved.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using UnityEngine;

public static class ValueClassExtensions {
	public static int GetHashCodeFromProperties(this object obj) {
		return Hasher.GetHashCode(obj
			.GetType()
			.GetProperties()
			.Select(propertyInfo => propertyInfo.GetValue(obj))
			.ToArray());
	}

	public static bool EqualityFromProperties(this object obj, object other) {
		if (other == null) {
			return false;
		}
		var type = obj.GetType();
		return (other.GetType().Equals(type)) &&
			type.GetProperties()
				.All(info => {
					var thisValue = info.GetValue(obj);
					var otherValue = info.GetValue(other);
					return thisValue == otherValue ||
						(thisValue != null && thisValue.Equals(otherValue)) ||
						enumerableEquality(thisValue, otherValue);
				});
	}

	private static bool enumerableEquality(object obj, object other) {
		var enumerable  = obj as IEnumerable;
		var otherEnumerable = other as IEnumerable;
		if (enumerable == null || otherEnumerable == null) {
			return false;
		}
		var castedEnumerable = enumerable.Cast<object>();
		var otherCastedEnumerable = otherEnumerable.Cast<object>();
		return castedEnumerable.SequenceEqual(otherCastedEnumerable);
	}

	public static string ToStringFromProperties(this object obj) {
		var stringBuilder = new StringBuilder();
		var type = obj.GetType();
		stringBuilder.AppendLine($"{type}:");
		foreach (var property in type.GetProperties()) {
			stringBuilder.AppendLine($"{property.Name}: {getStringDescription(property.GetValue(obj))}");
		}
		return stringBuilder.ToString();
	}

	private static string getStringDescription(object obj) {
		if (obj == null) {
			return "null";
		}

		if (obj is string) {
			return obj.ToString();
		}
		var enumerable = obj as IEnumerable;
		if (enumerable == null) {
			return obj.ToString();
		}
		return enumerable.Cast<object>().ToJoinedString(", ");
	}

	public static T ToObject<T>(this IDictionary<string, object> source) where T : class {
		var constructors = typeof(T).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).ToList();
		var constructor = constructors.FirstOrDefault(constructorInfo => constructorInfo.GetParameters().Length == source.Count);
		var caseInsensitiveDictionary = new Dictionary<string, object>(source, StringComparer.OrdinalIgnoreCase);
		if (constructor == null) {
			return initializeWithoutConstructor<T>(caseInsensitiveDictionary);
		}
		return initializeWithConstructor<T>(caseInsensitiveDictionary, constructor);
	}

	private static T initializeWithoutConstructor<T>(Dictionary<string, object> caseInsensitiveDictionary) where T : class {
		var type = typeof(T);
		var newObject = FormatterServices.GetUninitializedObject(type) as T;
		var fields = type.GetRuntimeFields();
		foreach(var field in fields) {
			object value;
			var fieldName = field.Name;
			var prefixEnd = fieldName.IndexOf('<');
			var postfixStart = fieldName.IndexOf('>');
			fieldName = fieldName.Substring(prefixEnd + 1, postfixStart - prefixEnd - 1);
			if (!caseInsensitiveDictionary.TryGetValue(fieldName, out value)) {
				continue;
			}

			field.SetValue(newObject, value);
		}
		return newObject;
	}

	private static T initializeWithConstructor<T>(IDictionary<string, object> caseInsensitiveDictionary, 
		ConstructorInfo constructor) {
		var constructorArguments = constructor.GetParameters()
			.Select(parameter => caseInsensitiveDictionary.Get(parameter.Name))
			.ToArray();

		return (T)constructor.Invoke(constructorArguments);
	}

	public static IDictionary<string, object> AsDictionary(this object source, BindingFlags bindingAttr = BindingFlags.Public | BindingFlags.Instance) {
		return source.GetType().GetProperties(bindingAttr).ToDictionary(
				propInfo => propInfo.Name,
				propInfo => propInfo.GetValue(source, null)
		);
	}

	// TODO - try implement this and CopyWithModifiedValue using this: https://stackoverflow.com/questions/8523061/how-to-verify-whether-a-type-overloads-supports-a-certain-operator
	public static T CopyWithModifiedValues<T>(
		this T source,
		Dictionary<string, int> values) where T : class {
		var dictionary = source.AsDictionary();
		values.ForEach(pair => {
			var value = (int)dictionary.Get(pair.Key, typeof(T).ToString());
			dictionary[pair.Key] = value + pair.Value;
		});

		return dictionary.ToObject<T>();
	}

	public static T CopyWithModifiedValue<T>(
		this T source,
		string propertyName,
		int propertyValue) where T : class {
		var dictionary = source.AsDictionary();
		var value = (int)dictionary.Get(propertyName, typeof(T).ToString());
		dictionary[propertyName] = value + propertyValue;
		return dictionary.ToObject<T>();
	}

	public static T CopyWithModifiedValue<T>(
		this T source,
		string propertyName,
		float propertyValue) where T : class {
		var dictionary = source.AsDictionary();
		var value = (float)dictionary.Get(propertyName, typeof(T).ToString());
		dictionary[propertyName] = value + propertyValue;
		return dictionary.ToObject<T>();
	}

	public static T CopyWithModifiedValue<T>(
		this T source,
		string propertyName,
		double propertyValue) where T : class {
		var dictionary = source.AsDictionary();
		var value = (double)dictionary.Get(propertyName, typeof(T).ToString());
		dictionary[propertyName] = value + propertyValue;
		return dictionary.ToObject<T>();
	}

	public static T CopyWithModifiedValue<T>(
		this T source,
		string propertyName,
		long propertyValue) where T : class {
		var dictionary = source.AsDictionary();
		var value = (long)dictionary.Get(propertyName, typeof(T).ToString());
		dictionary[propertyName] = value + propertyValue;
		return dictionary.ToObject<T>();
	}

	public static T CopyWithSetValue<T>(
		this T source, 
		string propertyName, 
		object propertyValue) where T : class {
		var dictionary = source.AsDictionary();
		AssertUtils.AssertConditionMet(dictionary.ContainsKey(propertyName), 
			$"{propertyName} not found");
		dictionary[propertyName] = propertyValue;
		return dictionary.ToObject<T>();
	}
}

public class BaseValueClass {
	public override bool Equals(object obj) {
		return this.EqualityFromProperties(obj);
	}

	public override int GetHashCode() {
		return this.GetHashCodeFromProperties();
	}

	public override string ToString() {
		return this.ToStringFromProperties();
	}

	public T ShallowClone<T>() {
		AssertUtils.AreEqual(typeof(T), GetType());
		return (T)MemberwiseClone();
	}
}
