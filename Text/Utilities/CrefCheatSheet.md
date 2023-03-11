#### CREF Notes

- `Types`

```xml
<!-- To refer to a specific instance of a generic type -->
<see cref="System.Span{T}">Span&lt;char&gt;</see>
```



```c#
using System;

namespace CrefCheatSheet;


/// <see cref="ICrefInterface"/>
/// <br/>
/// <see cref="T:CrefCheatSheet.ICrefInterface"/>
interface ICrefInterface
{
    /// <see cref="Method1"/>
    /// <br/>
    /// <see cref="ICrefInterface.Method1"/>
    /// <br/>
    /// <see cref="M:CrefCheatSheet.ICrefInterface.Method1(System.Int32)"/>
    void Method1(int intParam);

    /// <see cref="Method2{TU}"/>~~~~
    /// <br/>
    /// <see cref="M:CrefCheatSheet.ICrefInterface.Method2``1(``0)"/>
    void Method2<TU>(TU uParam);
    
    /// <see cref="Method3"/>
    /// <br/>
    /// <see cref="M:CrefCheatSheet.ICrefInterface.Method3(System.Action{System.String})"/>
    void Method3(Action<string> stringAction);

}

/// <see cref="ICrefInterfaceT{T}"/>
/// <br/>
/// <see cref="T:CrefCheatSheet.ICrefInterfaceT`1"/>
interface ICrefInterfaceT<T>
{
    /// <see cref="Method1"/>
    /// <br/>
    /// <see cref="M:CrefCheatSheet.ICrefInterfaceT`1.Method1(System.Int32)"/>
    void Method1(int intParam);

    /// <see cref="Method2"/>
    /// <br/>
    /// <see cref="M:CrefCheatSheet.ICrefInterfaceT`1.Method2(`0)"/>
    void Method2(T tParam);
    
    /// <see cref="Method3{U}"/>
    /// <br/>
    /// <see cref="M:CrefCheatSheet.ICrefInterfaceT`1.Method3``1(``0)"/>
    void Method3<U>(U uParam);

}

/// <see cref="ICrefIntInterface"/>
/// <br/>
/// <see cref="ICrefInterfaceT{T}"/>
/// <br/>
/// <see cref="T:CrefCheatSheet.ICrefInterfaceT`1"/>
/// <br/>
/// <see cref="T:CrefCheatSheet.ICrefInterfaceT&lt;T&gt;"/>
/// <br/>
/// <see cref=""/>
/// <br/>
interface ICrefIntInterface : ICrefInterfaceT<int>
{
    
}
```

