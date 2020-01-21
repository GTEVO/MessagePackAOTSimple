# MessagePackAOTSimple

服务器端使用 nuget 安装<br/>
unity 中使用 MessagePack 发布的release包<br/>

## 问题
当服务器和客户端要使用同一个库中定义的消息时，则该消息需要添加 MessagePackObject 属性，需要引用 MessagePack。这种情况下，引用任何一方的 MessagePack 都会造成另一方不可用

## 解决
引入一个“引用程序集”，依赖它通过编译，运行时各自仍使用自身的 MessagePack 库。在这个程序集里需要定义MessagePack里的自定义属性类，例如MessagePackObjectAttribute等。本示例中，输出名为 MessagePack.Annotations，与服务器nuget中的MessagePack结构有关。相应的，unity中也需要定义一个名为MessagePack.Annotations程序集引用文件。
en...... 专走邪门歪道 ^~^

## 提示
首次打开Unity项目会报错，无视即可

## Mark
reed solomon FEC+KCP (https://github.com/skywind3000/kcp/issues/38)