class ObjectExtensions
{
	static getHashCode(value)
	{
		var hash = 0;
		const string = value?.toString()?.trim();
		if(ObjectExtensions.isNotEmptyString(string))
		{
			for(let char = 0; char < string.length; char++)
			{
				hash = (((hash << 5) - hash) + string.charCodeAt(char)) & 0xFFFFFFFF;
			}
		}
		return hash
	}
	static hasBasePrototypeOfObject(childObject, parentObject)
	{
		const prototypeParent = Object.getPrototypeOf(parentObject);
		return this.hasBasePrototype(childObject, prototypeParent);
	}
	static hasBasePrototype(object, basePrototype)
	{
		const prototype = Object.getPrototypeOf(object);
		return typeof prototype === "object" && prototype != null && (prototype === basePrototype || this.hasBasePrototype(prototype, basePrototype));
	}
	static hasBasePrototypeName(object, prototypeName)
	{
		const prototype = Object.getPrototypeOf(object);
		return this.isNotEmptyString(prototypeName) && prototype != null && (prototype.constructor?.name === prototypeName
			|| ObjectExtensions.hasBasePrototypeName(prototype, prototypeName));
	}
	static instanceof(object, constructor)
	{
		const prototype = Object.getPrototypeOf(object);
		return typeof prototype === "object" && prototype != null && (prototype.constructor === constructor || this.instanceOf(prototype, constructor));
	}
	static isEmptyString(object)
	{
		return typeof object === "undefined" || object === null || typeof object === "string" && object.trim().length === 0;
	}
	static isNotEmptyString(object)
	{
		return typeof object === "string" && object.trim().length > 0;
	}
	static isEmptyKey(key)
	{
		return this.isEmptyValue(key);
	}
	static isNotEmptyKey(key)
	{
		return this.isNotEmptyValue(key);
	}
	static isEmptyValue(value, includeFunc = true)
	{
		return typeof value === "string" && value.trim().length === 0 || typeof value === "undefined" || includeFunc && typeof value === "function" || value === null;
	}
	static isNotEmptyValue(value, excludeFunc = true)
	{
		return typeof value === "string" && value.trim().length > 0 || typeof value !== "undefined" && (!excludeFunc || typeof value !== "function") && value !== null;
	}
	static isValidIdentifier(value)
	{
		return typeof value === "string" && /[A-Za-z_$][\w$]*/.test(value.trim());
	}
	static addPropertyAccessors(target)
	{
		var result = (typeof target === "object" || typeof target === "function") && Object.keys(target)?.length > 0;
		var descriptors = undefined;
		if(result && Object.values(Object.getOwnPropertyDescriptors(target))?.some(descriptor => typeof descriptor.get !== "function"
			|| typeof descriptor.set !== "function") === true)
		{
			const proxy = new Proxy(target,
				{
					get(target, property, receiver)
					{
						var result = undefined;
						if(property === Symbol.iterator)
						{
							result = target[property].apply(target);
						}
						else if(typeof target[property] === "function")
						{
							result = function(...args)
							{
								return target[property].apply(target, args);
							};
						}
						else if(property in target)
						{
							result = Reflect.get(target, property, ...arguments);
						}
						else if(typeof target.get === "function" && target.get.length === 1)
						{
							result = target.get(property);
						}
						return result;
					},
					set(target, property, value, receiver)
					{
						var result = false;
						if(typeof target[property] === "function")
						{
							result = function(...args)
							{
								target[property].apply(target, args);
								return true;
							}
						}
						else if(property in target)
						{
							result = Reflect.set(target, property, value, ...arguments);
						}
						else if(typeof target.set === "function" && target.set.length === 2)
						{
							target.set(property, value);
							result = true;
						}
						return result;
					}
				});
			if(result = typeof proxy === "object")
			{
				target = proxy;
			}
		}
		return result;
	}
	static toQueryString(object)
	{
		var entries = null;
		if(typeof object === "object" && !$.isEmptyObject(object))
		{
			entries = Object.entries(object)?.filter(([key, value]) => ObjectExtensions.isNotEmptyString(key) && (typeof value === "boolean"
				|| Number.isInteger(value) || ObjectExtensions.isNotEmptyString(value)));
		}
		return new URLSearchParams(entries);
	}
}

class Enum
{
	static parse(value, type)
	{
		var result = NaN;
		try
		{
			const classOfEnum = Enum.#getEnumClass(type ?? this);
			if(typeof classOfEnum === "function")
			{
				var pair = { Key: null, Value: undefined };
				if(typeof value === "string")
				{
					pair.Key = value.trim();
				}
				else if(typeof value === "number")
				{
					const parsed = Number.parseInt(value);
					if(!Number.isNaN(parsed))
					{
						pair.Value = parsed;
					}
				}
				if(typeof pair.Key === "string" || typeof pair.Value === "number")
				{
					const arrEnumKeys = Enum.keys(classOfEnum);
					if(arrEnumKeys?.length > 0)
					{
						if(typeof pair.Key === "string")
						{
							if(arrEnumKeys?.includes(pair.Key) === true && typeof classOfEnum[pair.Key] === "number")
							{
								result = classOfEnum[pair.Key];
							}
						}
						else
						{
							const key = arrEnumKeys.find(curKey => classOfEnum[curKey] === pair.Value);
							if(typeof key === "string")
							{
								result = classOfEnum[key];
							}
						}
					}
				}
			}
		}
		catch(e)
		{
			console.error(e);
			throw e;
		}
		return result;
	}
	static toString(value, type)
	{
		var result = null;
		try
		{
			const classOfEnum = Enum.#getEnumClass(type ?? this);
			if(typeof classOfEnum === "function")
			{
				result = this.getKeyByValue(value, classOfEnum);
			}
		}
		catch(e)
		{
			console.error(e);
			throw e;
		}
		return result;
	}
	static isEnumOfType(value, parsed = false, type)
	{
		var result = false;
		try
		{
			const classOfEnum = Enum.#getEnumClass(type ?? this);
			if(typeof classOfEnum === "function")
			{
				const arrValues = Enum.values(classOfEnum);
				if(arrValues?.length > 0)
				{
					const newValue = parsed ? value : Enum.parse(value, classOfEnum);
					result = typeof newValue === "number" && Number.isInteger(newValue) && arrValues.includes(newValue);
				}
			}
		}
		catch(e)
		{
			console.error(e);
			throw e;
		}
		return result;
	}
	static entries(type)
	{
		var result = null;
		try
		{
			const classOfEnum = Enum.#getEnumClass(type ?? this);
			result = typeof classOfEnum === "function" ? Enum.keys(classOfEnum)?.map(key => [key, classOfEnum[key]])
				?.filter(([key, value]) => typeof value === "number") : null;
		}
		catch(e)
		{
			console.error(e);
			throw e;
		}
		return result;
	}
	static keys(type)
	{
		var result = null;
		try
		{
			const arrStdProperties = ["length", "name", "prototype"];
			const classOfEnum = Enum.#getEnumClass(type ?? this);
			result = typeof classOfEnum === "function" ? Object.getOwnPropertyNames(classOfEnum)?.filter(curName => ObjectExtensions.isNotEmptyString(curName)
				&& !arrStdProperties.includes(curName)) : null;
		}
		catch(e)
		{
			console.error(e);
			throw e;
		}
		return result;
	}
	static values(type)
	{
		var result = null;
		try
		{
			const classOfEnum = Enum.#getEnumClass(type ?? this);
			result = typeof classOfEnum === "function" ? Enum.keys(classOfEnum)?.map(key => classOfEnum[key])?.filter(value => typeof value === "number") : null;
			//?.toSorted() : null;
		}
		catch(e)
		{
			console.error(e);
			throw e;
		}
		return result;
	}
	static containsKey(key, type)
	{
		var result = false;
		try
		{
			const classOfEnum = Enum.#getEnumClass(type ?? this);
			result = typeof classOfEnum === "function" ? Enum.entries(classOfEnum)?.some(([curKey, curValue]) => curKey === key) ?? false : false;
		}
		catch(e)
		{
			console.error(e);
			throw e;
		}
		return result;
	}
	static getKeyByValue(value, type)
	{
		var result = undefined;
		try
		{
			const classOfEnum = Enum.#getEnumClass(type ?? this);
			result = typeof classOfEnum === "function" ? (([key, value]) => key)(Enum.entries(classOfEnum)?.find(([curKey, curValue]) => curValue === value) ?? []) :
				undefined;
		}
		catch(e)
		{
			console.error(e);
			throw e;
		}
		return result;
	}
	static getValueByKey(key, type)
	{
		var result = undefined;
		try
		{
			const classOfEnum = Enum.#getEnumClass(type ?? this);
			result = typeof classOfEnum === "function" ? (([key, value]) => value)(Enum.entries(classOfEnum)?.find(([curKey, curValue]) => curKey === key) ?? []) :
				undefined;
		}
		catch(e)
		{
			console.error(e);
			throw e;
		}
		return result;
	}
	static #getEnumClass(type)
	{
		var classOfEnum = undefined;
		try
		{
			classOfEnum = typeof type === "function" ? type : typeof type === "string" && typeof globalThis[type] === "function" ? globalThis[type] : undefined;
			if((typeof classOfEnum !== "function" || typeof classOfEnum.prototype?.constructor !== "function"
				|| classOfEnum.prototype.constructor === Enum.prototype.constructor) && typeof this.prototype?.constructor === "function"
				&& typeof this.prototype.constructor !== Enum.prototype.constructor)
			{
				classOfEnum = this.prototype.constructor;
			}
		}
		catch(e)
		{
			console.error(e);
			throw e;
		}
		return classOfEnum;
	}
}
