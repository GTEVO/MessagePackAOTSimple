# MessagePackAOTSimple

服务器端使用 nuget 安装<br/>
unity 中使用 MessagePack 发布的release包<br/>

## 问题
当服务器和客户端要使用同一个库中定义的消息时，则该消息需要添加 MessagePackObject 属性，需要引用 MessagePack。这种情况下，引用任何一方的 MessagePack 都会造成另一方不可用

## 解决
在unity的工程的msgpack Annotations 目录下创建一个MessagePack.Annotations定义文件，再拖到msgpack引用列表中

## 提示
首次打开Unity项目会报错，无视即可

## Mark
reed solomon FEC+KCP (https://github.com/skywind3000/kcp/issues/38)
http://mauve.mizuumi.net/2012/07/05/understanding-fighting-game-networking/
