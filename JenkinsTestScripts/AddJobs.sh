#COUNTER=6
#while [  $COUNTER -lt 7 ]; do
#	curl -X POST -H "Content-Type:application/xml" -d @config.xml "http://localhost:8080/createItem?name=Item$COUNTER"
#	let COUNTER=COUNTER+1 
#done
#echo "ready"
#read

#placeholder="RepoName"
#for dr in */ ; do
#	currentDirName=${dr%/}
#	
#	sed -i "s/$placeholder/$currentDirName/g" config.xml
#	placeholder=$currentDirName
#	
#	curl -X POST -H "Content-Type:application/xml" -d @config.xml "http://localhost:8080/createItem?name=$currentDirName"
#	
#	read
#done
curl -X POST -H "Content-Type:application/xml" -d @config.xml "http://localhost:8080/createItem?name=18+"

#sed -i "s/$placeholder/RepoName/g" testFile.txt

echo ready
read