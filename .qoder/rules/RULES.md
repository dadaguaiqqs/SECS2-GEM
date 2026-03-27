---
trigger: always_on
---

## 注释规范
- 使用 XML 文档注释为公共 API 编写文档：
  /// <summary>
  /// 获取指定用户的详细信息
  /// </summary>
  /// <param name="userId">用户唯一标识符</param>
  /// <returns>用户详细信息对象</returns>
  public User GetUserById(int userId)
- 使用单行注释 `//` 解释复杂的业务逻辑
- 使用多行注释 `/* */` 临时禁用代码块
- 注释应该解释"为什么"而不是"是什么"
- 保持注释与代码同步更新
- 避免冗余注释，代码应该自解释
- 对于复杂算法，在方法前添加详细说明
- TODO 注释格式：`// TODO: 描述待办事项`

# C# 开发规范和最佳实践

## 命名规范
- 类名使用 PascalCase：`public class UserService`
- 方法名使用 PascalCase：`public void GetUserData()`
- 私有字段使用 camelCase 或 _camelCase：`private string userName;` 或 `private string _userName;`
- 接口以 I 开头：`public interface IUserRepository`
- 常量使用 PascalCase：`public const int MaxRetryCount = 3;`

## 代码风格
- 使用 4 个空格缩进
- 大括号另起一行（Allman 风格）
- 每行代码不超过 120 个字符
- 使用 var 关键字当类型显而易见时

## 最佳实践
- 优先使用 LINQ 进行集合操作
- 使用 async/await 处理异步操作
- 实现 IDisposable 接口的类使用 using 语句
- 使用空值合并运算符：`var name = userName ?? "默认名称";`
- 优先使用字符串插值：`$"用户名：{userName}"`
- 使用记录类型（record）处理不可变数据
- 使用模式匹配简化类型检查

## 错误处理
- 使用具体的异常类型
- 避免捕获并忽略异常
- 在适当的层级处理异常
- 使用 try-catch-finally 确保资源释放

## 性能优化
- 使用 StringBuilder 拼接大量字符串
- 避免在循环中进行装箱/拆箱操作
- 使用 Span<T> 和 Memory<T> 处理内存密集型操作
- 合理使用缓存机制

## 其他

-  写完代码不要进行编译
- 编译时使用msbuild编译 msbuild.exe的位置： D:\VS2022\IDE\MSBuild\Current\Bin
- 使用第三方库的时候，先检测一下项目里是否已经安装，没安装就安装。
- 代码的结构要清楚，相似功能的cs文件，放在同一个文件夹里，通过文件夹区分各种功能模块
- 生成的文件夹结构，需要符合开源文件夹的结构


### 注意 面向抽象
- 注重高内聚，低耦合
- 多使用 设计模式，使用的时候需要详细说明，为什么选择这个设计模式，以及好处是什么
- 对于抽象代码，需要详细说明 设计的思路，为什么这么设计 以及代码的执行流程
- 在使用一些代码 架构 的时候，也需要详细的说明，为什么选择这个架构，以及好处是什么

