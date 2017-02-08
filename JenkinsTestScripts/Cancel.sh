COUNTER=1
while [  $COUNTER -lt 1131 ]; do
	curl -X POST "http://localhost:8080/queue/cancelItem?id=$COUNTER"
	let COUNTER=COUNTER+1 
done
echo "ready"
read