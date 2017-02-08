COUNTER=8
while [  $COUNTER -lt 9 ]; do
	curl -X POST -H "Content-Type:application/xml" -d @config.xml "http://localhost:8080/view/All/createItem?createItem?name=Item$COUNTER"
	let COUNTER=COUNTER+1 
done
echo "ready"
read