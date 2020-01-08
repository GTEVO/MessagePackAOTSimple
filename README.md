# MessagePackAOTSimple

服务器端使用 nuget 安装
unity 中使用 MessagePack 发布的release包

## 问题
当服务器和客户端要使用同一个库中定义的消息时，则该消息需要添加 MessagePackObj 属性，需要引用 MessagePack。这种情况下，引用任何一方的 MessagePack 都会造成另一方不可用