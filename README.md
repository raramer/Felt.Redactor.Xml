# Redactor
Redactor is a set of libraries used to make it easy to mask sensitive information within structured text.

### How do I get started?

1. Configure the redactor.  In it's simplest form this is simply a list of element/attribute names whose values you want to mask.  Attributes are specified in the form "{element name}@{attribute name}".
    ```csharp
    var redactor = new XmlRedactor("password", "username@token");
    ```

2. Call the Redact method with the xml structured text.
    ```csharp
    var xml = @"<user>
                   <username token=""1234"">jdoe</username>
                   <password>P@ssw0rd!</password>
                </user>";
    var redactedXml = redactor.Redact(xml); 
    ```
    ```
    <user><username token="[REDACTED]">jdoe</username><password>[REDACTED]</password></user>
    ```

### Options

* Redacts : a list of elements and/or attributes whose values need to be masked.

    See example above.

* IfIsRedacts : a list of rules for optional masking.

  * If : the name of an element/attribute you want to check.
  * Is : the expected value of the If element/attribute to enable masking of the Redact field.
  * Redact: the name of a field whose value needs to be masked. This can either be the If element/attribute or one of its siblings.

    ```csharp
    var redactor = new XmlRedactor(new IfIsRedact 
    { 
        If = "name", 
        Is = "password", 
        Redact = "value" 
    });

    var xml = @"<user>
                    <property>
                        <name>username</name>
                        <value>jdoe</value>
                    </property>
                    <property>
                        <name>password</name>
                        <value>P@ssw0rd!</value>
                    </property>
                </user>";
    var redactedXml = redactor.Redact(xml);
    ```
    ```
    <user><property><name>username</name><value>jdoe</value></property><property><name>password</name><value>[REDACTED]</value></property></user>
    ```

* Mask : the value used to mask redacted values. The default is "[REDACTED]", but you can provide the value you prefer.  e.g. "********"

* StringComparison : how to compare values.  The default is OrdinalIgnoreCase.

* ComplexTypeHandling : how to mask complex types.
  * RedactValue (default) : redacts the entire value.
    ```
    <transaction><total>19.99</total><creditCard>[REDACTED]</creditcard></transaction>
    ``` 
  * RedactDescendants : redacts each descendant value, preserving the complex type's structure.
    ```
    <transaction><total>19.99</total><creditCard><type>[REDACTED]</type><number>[REDACTED]</number><expiration>[REDACTED]</expiration><cvv>[REDACTED]</cvv></creditcard></transaction>
    ``` 

* OnErrorRedact : how to handle the response when the value does not conform to the structured text type.
  * All (default) : redacts the entire response.
  * None : the original value is returned.

* Formatting : how to format the response
  * Compressed (default) : unnecessary whitespace is removed.
    ```
    <user><username token=""[REDACTED]"">jdoe</username><password>[REDACTED]</password></user>
    ```
  * Indented : contains newlines and indented properties.
    ```
    <user>
      <username token=\"[REDACTED]\">jdoe</username>
      <password>[REDACTED]</password>
    </user>
    ```
